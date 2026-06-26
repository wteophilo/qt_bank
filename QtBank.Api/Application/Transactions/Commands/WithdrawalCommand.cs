using System;
using MediatR;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Command to request a money withdrawal from an account.
/// </summary>
public record WithdrawalCommand(
    string AccountNumber,
    decimal Amount,
    Currency Currency
) : IRequest<Result<TransferResponseDto>>;
