using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Application.Accounts.Queries;

public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountBalanceQueryHandler> _logger;

    public GetAccountBalanceQueryHandler(
        IAccountRepository repository,
        ILogger<GetAccountBalanceQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving balance for account number: {AccountNumber}", request.AccountNumber);

        var account = await _repository.GetByNumberAsync(request.AccountNumber, cancellationToken);
        if (account is null)
        {
            _logger.LogWarning("Account not found for account number: {AccountNumber}", request.AccountNumber);
            return null;
        }

        return new AccountBalanceDto(account.AccountNumber, account.Balance);
    }
}
