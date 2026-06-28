using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;

namespace QtBank.Api.Infrastructure.Outbox;

/// <summary>
/// Background worker that periodically polls the Outbox store for pending messages,
/// publishes them to the message broker, and marks them as processed.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Core execution loop of the hosted background processor.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Background Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the Outbox processor loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PullIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Background Processor stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPubSubPublisher>();

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation("Processing Outbox message {MessageId} of type {Type}.", message.Id, message.Type);

                // Resolve event type dynamically at runtime
                var type = Type.GetType(message.Type);
                if (type is null)
                {
                    throw new InvalidOperationException($"Could not resolve type: {message.Type}");
                }

                // Deserialize JSON payload into the concrete event type
                var payload = JsonSerializer.Deserialize(message.Content, type);
                if (payload is null)
                {
                    throw new InvalidOperationException($"Could not deserialize content for message: {message.Id}");
                }

                // Execute the publishing logic with retry policies
                await ExecutePublishWithRetryAsync(publisher, message, payload, type, cancellationToken);

                // Mark message as processed successfully
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}.", message.Id);
                message.Error = ex.ToString();
            }

            // Save the updated state (processed timestamp or error trace)
            await outboxRepository.UpdateAsync(message, cancellationToken);
        }
    }

    private async Task ExecutePublishWithRetryAsync(
        IPubSubPublisher publisher,
        OutboxMessage message,
        object payload,
        Type type,
        CancellationToken cancellationToken)
    {
        int maxRetryAttempts = _options.MaxRetryAttempts;
        int attempt = 0;
        bool success = false;

        while (!success && attempt < maxRetryAttempts)
        {
            try
            {
                attempt++;
                await PublishMessageGenericAsync(publisher, message.Topic, payload, type, cancellationToken);
                success = true;
            }
            catch (Exception ex) when (attempt < maxRetryAttempts)
            {
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId} on attempt {Attempt}. Retrying...", message.Id, attempt);
                
                // Exponential backoff: 100ms, 200ms, 400ms
                var delay = (int)Math.Pow(2, attempt) * 100;
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task PublishMessageGenericAsync(
        IPubSubPublisher publisher,
        string topic,
        object payload,
        Type type,
        CancellationToken cancellationToken)
    {
        // Retrieve generic PublishAsync method from publisher interface
        var method = typeof(IPubSubPublisher)
            .GetMethod(nameof(IPubSubPublisher.PublishAsync))
            ?.MakeGenericMethod(type);

        if (method is null)
        {
            throw new InvalidOperationException($"Generic PublishAsync method could not be resolved for type: {type.FullName}");
        }

        // Invoke: publisher.PublishAsync<T>(topic, payload, cancellationToken)
        var task = (Task)method.Invoke(publisher, new[] { topic, payload, cancellationToken })!;
        await task;
    }
}
