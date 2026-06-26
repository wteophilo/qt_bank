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

    /// <inheritdoc />
    public Task<IEnumerable<Transaction>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var result = new System.Collections.Generic.List<Transaction>();
        foreach (var tx in _transactions.Values)
        {
            if (tx.SourceAccountNumber.Equals(accountNumber, System.StringComparison.OrdinalIgnoreCase) ||
                tx.DestinationAccountNumber.Equals(accountNumber, System.StringComparison.OrdinalIgnoreCase))
            {
                result.Add(tx);
            }
        }
        return Task.FromResult<System.Collections.Generic.IEnumerable<Transaction>>(result);
    }
}
