using MediatR;
using QtBank.Api.Application.DTOs;

namespace QtBank.Api.Application.Accounts.Queries;

public record GetAccountBalanceQuery(string AccountNumber) : IRequest<AccountBalanceDto?>;
