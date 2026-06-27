using System;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// Data Transfer Object representing a bank transaction response.
/// </summary>
public record TransactionResponse(
    Guid Id,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency,
    string Type,
    Guid IdempotencyKey,
    string Status,
    DateTime CreatedAt
);
