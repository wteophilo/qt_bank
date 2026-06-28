namespace QtBank.Api.Infrastructure.Outbox;

/// <summary>
/// Configuration options for the Transactional Outbox processor.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// The maximum number of immediate retry attempts when publishing a message.
    /// Defaults to 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The interval in seconds between consecutive polling loops.
    /// Defaults to 1.
    /// </summary>
    public int PullIntervalSeconds { get; set; } = 1;
}
