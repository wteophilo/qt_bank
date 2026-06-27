using System;
using System.Text.Json.Serialization;
using MediatR;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Command to request a money deposit into an account.
/// </summary>
[method: JsonConstructor]
public record DepositCommand(
    string AccountNumber,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey
) : IRequest<Result<TransferResponse>>
{
    /// <summary>
    /// Overloaded constructor for compatibility with existing code and tests.
    /// </summary>
    public DepositCommand(string accountNumber, decimal amount, Currency currency)
        : this(accountNumber, amount, currency, Guid.NewGuid())
    {
    }
}
