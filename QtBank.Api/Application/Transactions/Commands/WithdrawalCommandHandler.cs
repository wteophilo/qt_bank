using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using QtBank.Api.Application.Common;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Events;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Command handler for processing account withdrawal requests.
/// </summary>
public class WithdrawalCommandHandler : IRequestHandler<WithdrawalCommand, Result<TransferResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<WithdrawalCommandHandler> _logger;

    public WithdrawalCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IPubSubPublisher publisher,
        ILogger<WithdrawalCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<TransferResponse>> Handle(WithdrawalCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing WithdrawalCommand for account {AccountNumber}", request.AccountNumber);

        // 0. Check Idempotency
        var existingTx = await _transactionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingTx is not null)
        {
            _logger.LogInformation("Withdrawal already processed for idempotency key {Key}", request.IdempotencyKey);
            return Result<TransferResponse>.Ok(new TransferResponse(existingTx.Id, existingTx.Status.ToString(), existingTx.CreatedAt));
        }

        // 1. Fetch Account
        var account = await _accountRepository.GetByNumberAsync(request.AccountNumber, cancellationToken);
        if (account is null)
        {
            _logger.LogError("Withdrawal failed: Account not found. {Account}", request.AccountNumber);
            return Result<TransferResponse>.Fail("Account not found.");
        }

        // 2. Validate active status
        if (!account.IsActive())
        {
            _logger.LogError("Withdrawal failed: Account is not active. {Account}", request.AccountNumber);
            return Result<TransferResponse>.Fail("Account is not active.");
        }

        // 3. Validate sufficient funds
        if (!account.CanDebit(request.Amount))
        {
            _logger.LogError("Withdrawal failed: Insufficient funds in {Account}.", request.AccountNumber);
            return Result<TransferResponse>.Fail("Insufficient funds.");
        }

        // 4. Update Balance
        account.Debit(request.Amount);
        await _accountRepository.SaveAsync(account, cancellationToken);

        // 5. Create & Persist Transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = request.AccountNumber,
            DestinationAccountNumber = string.Empty,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Withdrawal,
            IdempotencyKey = request.IdempotencyKey,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var savedTx = await _transactionRepository.SaveAsync(transaction, cancellationToken);

        // 6. Publish integration event
        await PublishWithdrawalCompletedEventAsync(savedTx, cancellationToken);

        return Result<TransferResponse>.Ok(new TransferResponse(savedTx.Id, savedTx.Status.ToString(), savedTx.CreatedAt));
    }

    private async Task PublishWithdrawalCompletedEventAsync(Transaction tx, CancellationToken cancellationToken)
    {
        var ev = new WithdrawalCompleted(
            tx.Id,
            tx.SourceAccountNumber,
            tx.Amount,
            tx.Currency.ToString(),
            tx.IdempotencyKey,
            tx.Status.ToString(),
            tx.CreatedAt
        );
        await _publisher.PublishAsync("withdrawals-topic", ev, cancellationToken);
    }
}
