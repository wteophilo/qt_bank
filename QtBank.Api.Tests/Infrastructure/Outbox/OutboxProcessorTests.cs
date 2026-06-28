using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QtBank.Api.Domain.Events;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;
using QtBank.Api.Infrastructure.Outbox;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Outbox;

/// <summary>
/// Unit tests for the <see cref="OutboxProcessor"/> background worker.
/// </summary>
public class OutboxProcessorTests
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _publisher = Substitute.For<IPubSubPublisher>();
        _logger = Substitute.For<ILogger<OutboxProcessor>>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepository);
        _serviceProvider.GetService(typeof(IPubSubPublisher)).Returns(_publisher);

        var options = Options.Create(new OutboxOptions { MaxRetryAttempts = 3, PullIntervalSeconds = 1 });
        _processor = new OutboxProcessor(_scopeFactory, _logger, options);
    }

    [Fact]
    public async Task ProcessOutboxMessages_WhenMessagePending_ShouldPublishAndMarkAsProcessed()
    {
        // Arrange
        var ev = new TransferCompleted(Guid.NewGuid(), "111111", "222222", 100m, "USD", Guid.NewGuid(), "Completed", DateTime.UtcNow);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TransferCompleted).AssemblyQualifiedName!,
            Topic = "transfers-topic",
            Content = JsonSerializer.Serialize(ev),
            OccurredOnUtc = DateTime.UtcNow
        };

        var messages = new List<OutboxMessage> { outboxMessage };
        _outboxRepository.GetUnprocessedMessagesAsync(Arg.Any<CancellationToken>()).Returns(messages);

        // Act
        // Invoke target protected method 'ProcessOutboxMessagesAsync' via reflection for testing
        var method = typeof(OutboxProcessor).GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        await (Task)method!.Invoke(_processor, new object[] { CancellationToken.None })!;

        // Assert
        // 1. Verify generic PublishAsync was called on publisher interface
        await _publisher.Received(1).PublishAsync(
            "transfers-topic",
            Arg.Is<TransferCompleted>(e => e.TransactionId == ev.TransactionId),
            Arg.Any<CancellationToken>()
        );

        // 2. Verify outbox message was updated and marked as processed
        await _outboxRepository.Received(1).UpdateAsync(
            Arg.Is<OutboxMessage>(m => m.Id == outboxMessage.Id && m.ProcessedOnUtc != null && m.Error == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ProcessOutboxMessages_WhenPublishingFails_ShouldRetryAndRecordError()
    {
        // Arrange
        var ev = new TransferCompleted(Guid.NewGuid(), "111111", "222222", 100m, "USD", Guid.NewGuid(), "Completed", DateTime.UtcNow);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TransferCompleted).AssemblyQualifiedName!,
            Topic = "transfers-topic",
            Content = JsonSerializer.Serialize(ev),
            OccurredOnUtc = DateTime.UtcNow
        };

        var messages = new List<OutboxMessage> { outboxMessage };
        _outboxRepository.GetUnprocessedMessagesAsync(Arg.Any<CancellationToken>()).Returns(messages);

        // Configure publisher to fail
        _publisher.PublishAsync<TransferCompleted>(Arg.Any<string>(), Arg.Any<TransferCompleted>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Broker offline"));

        // Act
        var method = typeof(OutboxProcessor).GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(_processor, new object[] { CancellationToken.None })!;

        // Assert
        // 1. Verify it attempted to publish 3 times (due to retry limit)
        await _publisher.Received(3).PublishAsync(
            "transfers-topic",
            Arg.Is<TransferCompleted>(e => e.TransactionId == ev.TransactionId),
            Arg.Any<CancellationToken>()
        );

        // 2. Verify outbox message was updated, ProcessedOnUtc is still null, and Error holds exception info
        await _outboxRepository.Received(1).UpdateAsync(
            Arg.Is<OutboxMessage>(m => m.Id == outboxMessage.Id && m.ProcessedOnUtc == null && m.Error!.Contains("Broker offline")),
            Arg.Any<CancellationToken>()
        );
    }
}
