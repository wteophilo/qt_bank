using System;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// Data transfer object for P2P transfer response.
/// </summary>
/// <param name="TransactionId">The unique ID generated for this transaction.</param>
/// <param name="Status">The processing status of the transaction (e.g., "Processing").</param>
/// <param name="Timestamp">The timestamp when the transaction was registered.</param>
public record TransferResponse(
    Guid TransactionId,
    string Status,
    DateTime Timestamp
);
