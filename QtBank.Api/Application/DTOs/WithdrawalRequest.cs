using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for withdrawing money from an account.
/// </summary>
[method: JsonConstructor]
public record WithdrawalRequest(
    string AccountNumber,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey
)
{
    /// <summary>
    /// Overloaded constructor for compatibility with existing code and tests.
    /// </summary>
    public WithdrawalRequest(string accountNumber, decimal amount, Currency currency)
        : this(accountNumber, amount, currency, Guid.NewGuid())
    {
    }
}
