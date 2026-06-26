using System;
using System.Text.Json.Serialization;
using MediatR;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Command to request a money transfer between two accounts.
/// </summary>
[method: JsonConstructor]
public record TransferCommand(
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey
) : IRequest<Result<TransferResponseDto>>
{
    /// <summary>
    /// Overloaded constructor for compatibility with existing code and tests.
    /// </summary>
    public TransferCommand(string sourceAccountNumber, string destinationAccountNumber, decimal amount, Currency currency)
        : this(sourceAccountNumber, destinationAccountNumber, amount, currency, Guid.NewGuid())
    {
    }
}
