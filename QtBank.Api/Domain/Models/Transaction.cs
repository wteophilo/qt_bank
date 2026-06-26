using System;

namespace QtBank.Api.Domain.Models;

/// <summary>
/// Domain model representing a P2P money transfer transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier of the transaction.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the source bank account.
    /// </summary>
    public string SourceAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the destination bank account.
    /// </summary>
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Amount of money transferred.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency of the transaction (e.g. "BRL", "USD").
    /// </summary>
    public Currency Currency { get; set; }

    /// <summary>
    /// Type of the transaction (Transfer, Deposit, Withdrawal).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Idempotency key used to prevent duplicate operations.
    /// </summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>
    /// Current processing status of the transaction.
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Processing;

    /// <summary>
    /// The timestamp when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
