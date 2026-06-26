using System;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Domain.Repositories;

/// <summary>
/// Repository interface for Transaction entities.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Saves or updates a transaction in the database.
    /// </summary>
    Task<Transaction> SaveAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by its idempotency key.
    /// </summary>
    Task<Transaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions associated with an account number (either as source or destination).
    /// </summary>
    Task<IEnumerable<Transaction>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
}
