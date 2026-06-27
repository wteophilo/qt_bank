using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for withdrawing money from an account.
/// </summary>
/// <param name="AccountNumber" example="222222">The account number to withdraw money from.</param>
/// <param name="Amount" example="50.00">The amount of money to withdraw.</param>
/// <param name="Currency" example="USD">The currency of the transaction (USD, EUR, BRL).</param>
/// <param name="IdempotencyKey" example="6fa85f64-5717-4562-b3fc-2c963f66afa6">A unique GUID to identify the request and prevent duplicate withdrawals.</param>
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
