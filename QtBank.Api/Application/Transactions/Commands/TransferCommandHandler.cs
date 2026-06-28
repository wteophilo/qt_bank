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
/// Command handler for processing P2P money transfer requests.
/// </summary>
public class TransferCommandHandler : IRequestHandler<TransferCommand, Result<TransferResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOutboxRepository outboxRepository,
        ILogger<TransferCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<Result<TransferResponse>> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing TransferCommand from {Source} to {Dest}", request.SourceAccountNumber, request.DestinationAccountNumber);

        // 0. Check Idempotency
        var existingTx = await _transactionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingTx is not null)
        {
            _logger.LogInformation("Transfer already processed for idempotency key {Key}", request.IdempotencyKey);
            return Result<TransferResponse>.Ok(new TransferResponse(existingTx.Id, existingTx.Status.ToString(), existingTx.CreatedAt));
        }

        // 1. Fetch Accounts
        var sourceAccount = await _accountRepository.GetByNumberAsync(request.SourceAccountNumber, cancellationToken);
        var destinationAccount = await _accountRepository.GetByNumberAsync(request.DestinationAccountNumber, cancellationToken);

        // 2. Validations
        var validationResult = ValidateTransferRules(sourceAccount, destinationAccount, request);
        if (validationResult is not null) return validationResult.Value;

        // 3. Execute & Persist
        var savedTx = await ExecuteAndPersistTransferAsync(sourceAccount!, destinationAccount!, request, cancellationToken);

        // 4. Save Outbox Message
        await SaveOutboxMessageAsync(savedTx, cancellationToken);

        return Result<TransferResponse>.Ok(new TransferResponse(savedTx.Id, savedTx.Status.ToString(), savedTx.CreatedAt));
    }

    private Result<TransferResponse>? ValidateTransferRules(Account? source, Account? destination, TransferCommand request)
    {
        if (source is null)
        {
            _logger.LogError("Transfer failed: Source account not found. {Account}", request.SourceAccountNumber);
            return Result<TransferResponse>.Fail("Source account not found.");
        }

        if (destination is null)
        {
            _logger.LogError("Transfer failed: Destination account not found. {Account}", request.DestinationAccountNumber);
            return Result<TransferResponse>.Fail("Destination account not found.");
        }

        if (!source.IsActive())
            return Result<TransferResponse>.Fail("Source account is not active.");

        if (!destination.IsActive())
            return Result<TransferResponse>.Fail("Destination account is not active.");

        if (!source.CanDebit(request.Amount))
        {
            _logger.LogError("Transfer failed: Insufficient funds in {Account}.", request.SourceAccountNumber);
            return Result<TransferResponse>.Fail("Insufficient funds in source account.");
        }

        return null;
    }

    private async Task<Transaction> ExecuteAndPersistTransferAsync(Account source, Account destination, TransferCommand request, CancellationToken cancellationToken)
    {
        source.Debit(request.Amount);
        destination.Credit(request.Amount);

        await _accountRepository.SaveAsync(source, cancellationToken);
        await _accountRepository.SaveAsync(destination, cancellationToken);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = request.SourceAccountNumber,
            DestinationAccountNumber = request.DestinationAccountNumber,
            Amount = request.Amount,
            Currency = request.Currency,
            Type = TransactionType.Transfer,
            IdempotencyKey = request.IdempotencyKey,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        return await _transactionRepository.SaveAsync(transaction, cancellationToken);
    }

    private async Task SaveOutboxMessageAsync(Transaction tx, CancellationToken cancellationToken)
    {
        var ev = new TransferCompleted(tx.Id, tx.SourceAccountNumber, tx.DestinationAccountNumber, tx.Amount, tx.Currency.ToString(), tx.IdempotencyKey, tx.Status.ToString(), tx.CreatedAt);
        
        var message = new OutboxMessage
        {
            Type = typeof(TransferCompleted).AssemblyQualifiedName ?? typeof(TransferCompleted).FullName!,
            Topic = "transfers-topic",
            Content = System.Text.Json.JsonSerializer.Serialize(ev)
        };

        await _outboxRepository.SaveAsync(message, cancellationToken);
    }
}

