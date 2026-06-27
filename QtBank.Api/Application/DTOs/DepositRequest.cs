using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for depositing money into an account.
/// </summary>
[method: JsonConstructor]
public record DepositRequest(
    string AccountNumber,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey
)
{
    /// <summary>
    /// Overloaded constructor for compatibility with existing code and tests.
    /// </summary>
    public DepositRequest(string accountNumber, decimal amount, Currency currency)
        : this(accountNumber, amount, currency, Guid.NewGuid())
    {
    }
}
