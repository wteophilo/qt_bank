using System;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// Data Transfer Object representing a bank transaction.
/// </summary>
public record TransactionDto(
    Guid Id,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey,
    string Status,
    DateTime CreatedAt
);
