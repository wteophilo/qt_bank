using System;

namespace QtBank.Api.Domain.Models;

/// <summary>
/// Domain model representing a message in the Transactional Outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Unique identifier of the outbox message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The fully qualified type name of the event (used for deserialization).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The target messaging topic where the event should be published.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// The JSON serialized content of the event payload.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The UTC timestamp when the message was created.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The UTC timestamp when the message was successfully processed. Null if pending.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// The stack trace or description of the error if the processing fails.
    /// </summary>
    public string? Error { get; set; }
}
