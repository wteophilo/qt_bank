using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QtBank.Api.Infrastructure.Messaging;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Messaging;

[Collection("Sequential")]
public class InMemoryPubSubPublisherTests : IDisposable
{
    private readonly ILogger<InMemoryPubSubPublisher> _logger;
    private readonly InMemoryPubSubPublisher _publisher;

    public InMemoryPubSubPublisherTests()
    {
        _logger = Substitute.For<ILogger<InMemoryPubSubPublisher>>();
        _publisher = new InMemoryPubSubPublisher(_logger);
        _publisher.Clear(); // Ensure static queue is clean before each test
    }

    public void Dispose()
    {
        _publisher.Clear(); // Cleanup static queue after each test
    }

    public record TestMessage(string Content, int Value);

    [Fact]
    public async Task PublishAsync_ShouldEnqueueMessageAndLogInformation()
    {
        // Arrange
        var topic = "test-topic";
        var message = new TestMessage("hello", 42);

        // Act
        await _publisher.PublishAsync(topic, message);

        // Assert
        var published = _publisher.GetPublishedMessages();
        published.Should().ContainSingle();

        var publishedMsg = published.First();
        publishedMsg.Topic.Should().Be(topic);
        publishedMsg.MessageType.Should().Be(typeof(TestMessage));
        publishedMsg.Message.Should().Be(message);
        publishedMsg.SerializedPayload.Should().Be("{\"Content\":\"hello\",\"Value\":42}");
        publishedMsg.PublishedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));

        // Verify Logging was invoked
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Publishing message to topic 'test-topic'")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEnqueuedMessages()
    {
        // Arrange
        await _publisher.PublishAsync("topic1", new TestMessage("msg1", 1));
        await _publisher.PublishAsync("topic2", new TestMessage("msg2", 2));
        _publisher.GetPublishedMessages().Should().HaveCount(2);

        // Act
        _publisher.Clear();

        // Assert
        _publisher.GetPublishedMessages().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublishedMessages_ShouldReturnMessagesInPublishingOrder()
    {
        // Arrange
        var msg1 = new TestMessage("first", 1);
        var msg2 = new TestMessage("second", 2);

        // Act
        await _publisher.PublishAsync("topic-a", msg1);
        await _publisher.PublishAsync("topic-b", msg2);

        // Assert
        var list = _publisher.GetPublishedMessages().ToList();
        list.Should().HaveCount(2);
        
        list[0].Topic.Should().Be("topic-a");
        list[0].Message.Should().Be(msg1);

        list[1].Topic.Should().Be("topic-b");
        list[1].Message.Should().Be(msg2);
    }
}
