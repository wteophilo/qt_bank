using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Application.Transactions.Queries;

/// <summary>
/// Query handler for retrieving all transactions of a bank account.
/// </summary>
public class GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery, Result<IEnumerable<TransactionResponse>>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<GetAccountTransactionsQueryHandler> _logger;

    public GetAccountTransactionsQueryHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ILogger<GetAccountTransactionsQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<TransactionResponse>>> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving transactions for account number: {AccountNumber}", request.AccountNumber);

        var account = await _accountRepository.GetByNumberAsync(request.AccountNumber, cancellationToken);
        if (account is null)
        {
            _logger.LogWarning("Account not found for account number: {AccountNumber}", request.AccountNumber);
            return Result<IEnumerable<TransactionResponse>>.Fail($"Account with number '{request.AccountNumber}' not found.");
        }

        var transactions = await _transactionRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);

        var result = transactions.Select(t => new TransactionResponse(
            t.Id,
            t.SourceAccountNumber,
            t.DestinationAccountNumber,
            t.Amount,
            t.Currency.ToString(),
            t.Type.ToString(),
            t.IdempotencyKey,
            t.Status.ToString(),
            t.CreatedAt
        )).ToList();

        return Result<IEnumerable<TransactionResponse>>.Ok(result);
    }
}
