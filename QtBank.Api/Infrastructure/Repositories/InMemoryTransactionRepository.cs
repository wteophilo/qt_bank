using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of the transaction repository.
/// </summary>
public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<Guid, Transaction> _transactions = new();
    private readonly ConcurrentDictionary<Guid, Transaction> _byIdempotencyKey = new();

    /// <inheritdoc />
    public Task<Transaction> SaveAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _transactions[transaction.Id] = transaction;
        _byIdempotencyKey[transaction.IdempotencyKey] = transaction;
        return Task.FromResult(transaction);
    }

    /// <inheritdoc />
    public Task<Transaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        _byIdempotencyKey.TryGetValue(idempotencyKey, out var transaction);
        return Task.FromResult(transaction);
    }
}
