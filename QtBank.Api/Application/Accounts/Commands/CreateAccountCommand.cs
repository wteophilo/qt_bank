using MediatR;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.Accounts.Commands;

public record CreateAccountCommand(
    string AccountNumber,
    decimal Balance,
    string OwnerName,
    AccountStatus Status
) : IRequest<AccountDto>;
