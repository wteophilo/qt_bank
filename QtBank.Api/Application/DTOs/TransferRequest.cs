using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for transferring money between two accounts.
/// </summary>
[method: JsonConstructor]
public record TransferRequest(
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey
)
{
    /// <summary>
    /// Overloaded constructor for compatibility with existing code and tests.
    /// </summary>
    public TransferRequest(string sourceAccountNumber, string destinationAccountNumber, decimal amount, Currency currency)
        : this(sourceAccountNumber, destinationAccountNumber, amount, currency, Guid.NewGuid())
    {
    }
}
