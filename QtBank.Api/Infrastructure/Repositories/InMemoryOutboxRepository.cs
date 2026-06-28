using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of the Outbox repository.
/// </summary>
public sealed class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    /// <inheritdoc />
    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var unprocessed = _messages.Values
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .ToList();

        return Task.FromResult<IEnumerable<OutboxMessage>>(unprocessed);
    }

    /// <inheritdoc />
    public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }
}
