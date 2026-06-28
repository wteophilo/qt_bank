using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Domain.Repositories;

/// <summary>
/// Repository interface for storing and retrieving Outbox messages.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Persists a new Outbox message in the store.
    /// </summary>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending/unprocessed Outbox messages from the store.
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the state of an existing Outbox message (e.g. marking it as processed).
    /// </summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
