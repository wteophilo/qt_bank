using System;

namespace QtBank.Api.Domain.Events;

/// <summary>
/// Integration event published when a P2P transfer is completed successfully.
/// </summary>
/// <param name="TransactionId">The ID of the transaction.</param>
/// <param name="SourceAccountNumber">The number of the source account.</param>
/// <param name="DestinationAccountNumber">The number of the destination account.</param>
/// <param name="Amount">The transaction amount.</param>
/// <param name="Currency">The transaction currency.</param>
/// <param name="IdempotencyKey">The idempotency key associated with the transfer request.</param>
/// <param name="Status">The processing status.</param>
/// <param name="Timestamp">The timestamp of the event.</param>
public record TransferCompleted(
    Guid TransactionId,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey,
    string Status,
    DateTime Timestamp
);
