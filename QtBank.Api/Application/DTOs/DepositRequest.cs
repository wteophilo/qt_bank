using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for depositing money into an account.
/// </summary>
/// <param name="AccountNumber" example="111111">The account number to deposit money into.</param>
/// <param name="Amount" example="5000.00">The amount of money to deposit.</param>
/// <param name="Currency" example="BRL">The currency of the transaction (USD, EUR, BRL).</param>
/// <param name="IdempotencyKey" example="5fa85f64-5717-4562-b3fc-2c963f66afa6">A unique GUID to identify the request and prevent duplicate deposits.</param>
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
