using System;
using System.Text.Json.Serialization;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

/// <summary>
/// HTTP request body schema for transferring money between two accounts.
/// </summary>
/// <param name="SourceAccountNumber" example="222222">The source account number to transfer funds from.</param>
/// <param name="DestinationAccountNumber" example="111111">The destination account number to transfer funds to.</param>
/// <param name="Amount" example="49.50">The amount of money to transfer.</param>
/// <param name="Currency" example="USD">The currency of the transaction (USD, EUR, BRL).</param>
/// <param name="IdempotencyKey" example="b32454da-8785-48fa-86e0-3fb364673891">A unique GUID to identify the request and prevent duplicate transfers.</param>
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
