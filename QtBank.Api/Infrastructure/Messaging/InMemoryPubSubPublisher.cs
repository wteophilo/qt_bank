using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QtBank.Api.Infrastructure.Messaging;

public class InMemoryPubSubPublisher : IPubSubPublisher
{
    private readonly ILogger<InMemoryPubSubPublisher> _logger;
    private static readonly ConcurrentQueue<PublishedMessage> _publishedMessages = new();

    public InMemoryPubSubPublisher(ILogger<InMemoryPubSubPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string topicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        _logger.LogInformation("[GCP Pub/Sub Simulation] Publishing message to topic '{Topic}': {Payload}", topicName, json);

        _publishedMessages.Enqueue(new PublishedMessage(topicName, typeof(T), message, json, DateTimeOffset.UtcNow));

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<PublishedMessage> GetPublishedMessages() => _publishedMessages.ToArray();

    public void Clear() => _publishedMessages.Clear();
}

public record PublishedMessage(
    string Topic,
    Type MessageType,
    object Message,
    string SerializedPayload,
    DateTimeOffset PublishedAt
);
