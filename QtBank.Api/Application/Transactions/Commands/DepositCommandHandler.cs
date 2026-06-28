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
/// Command handler for processing account deposit requests.
/// </summary>
public class DepositCommandHandler : IRequestHandler<DepositCommand, Result<TransferResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<DepositCommandHandler> _logger;

    public DepositCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOutboxRepository outboxRepository,
        ILogger<DepositCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<Result<TransferResponse>> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DepositCommand for account {AccountNumber}", request.AccountNumber);

        // 0. Check Idempotency
        var existingTx = await _transactionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingTx is not null)
        {
            _logger.LogInformation("Deposit already processed for idempotency key {Key}", request.IdempotencyKey);
            return Result<TransferResponse>.Ok(new TransferResponse(existingTx.Id, existingTx.Status.ToString(), existingTx.CreatedAt));
        }

        // 1. Fetch Account
        var account = await _accountRepository.GetByNumberAsync(request.AccountNumber, cancellationToken);
        if (account is null)
        {
            _logger.LogError("Deposit failed: Account not found. {Account}", request.AccountNumber);
            return Result<TransferResponse>.Fail("Account not found.");
        }

        // 2. Validate active status
        if (!account.IsActive())
        {
            _logger.LogError("Deposit failed: Account is not active. {Account}", request.AccountNumber);
            return Result<TransferResponse>.Fail("Account is not active.");
        }

        // 3. Update Balance
        account.Credit(request.Amount);
        await _accountRepository.SaveAsync(account, cancellationToken);

        // 4. Create & Persist Transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = string.Empty,
            DestinationAccountNumber = request.AccountNumber,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Deposit,
            IdempotencyKey = request.IdempotencyKey,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var savedTx = await _transactionRepository.SaveAsync(transaction, cancellationToken);

        // 5. Save Outbox Message
        await SaveOutboxMessageAsync(savedTx, cancellationToken);

        return Result<TransferResponse>.Ok(new TransferResponse(savedTx.Id, savedTx.Status.ToString(), savedTx.CreatedAt));
    }

    private async Task SaveOutboxMessageAsync(Transaction tx, CancellationToken cancellationToken)
    {
        var ev = new DepositCompleted(
            tx.Id,
            tx.DestinationAccountNumber,
            tx.Amount,
            tx.Currency.ToString(),
            tx.IdempotencyKey,
            tx.Status.ToString(),
            tx.CreatedAt
        );
        
        var message = new OutboxMessage
        {
            Type = typeof(DepositCompleted).AssemblyQualifiedName ?? typeof(DepositCompleted).FullName!,
            Topic = "deposits-topic",
            Content = System.Text.Json.JsonSerializer.Serialize(ev)
        };

        await _outboxRepository.SaveAsync(message, cancellationToken);
    }
}
