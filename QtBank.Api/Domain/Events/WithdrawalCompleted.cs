using System;

namespace QtBank.Api.Domain.Events;

/// <summary>
/// Integration event published when a withdrawal is completed successfully.
/// </summary>
public record WithdrawalCompleted(
    Guid TransactionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey,
    string Status,
    DateTime Timestamp
);
