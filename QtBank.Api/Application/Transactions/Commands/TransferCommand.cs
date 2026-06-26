using System;
using MediatR;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Command to request a money transfer between two accounts.
/// </summary>
/// <param name="SourceAccountNumber">The account number from which money is debited.</param>
/// <param name="DestinationAccountNumber">The account number to which money is credited.</param>
/// <param name="Amount">The amount of money to transfer.</param>
/// <param name="Currency">The currency of the transaction (e.g. "BRL", "USD").</param>
public record TransferCommand(
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency
) : IRequest<Result<TransferResponseDto>>;
