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
public class TransferCommandHandler : IRequestHandler<TransferCommand, Result<TransferResponseDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IPubSubPublisher publisher,
        ILogger<TransferCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<TransferResponseDto>> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing TransferCommand from {Source} to {Dest}", request.SourceAccountNumber, request.DestinationAccountNumber);

        // 1. Fetch Accounts
        var sourceAccount = await _accountRepository.GetByNumberAsync(request.SourceAccountNumber, cancellationToken);
        var destinationAccount = await _accountRepository.GetByNumberAsync(request.DestinationAccountNumber, cancellationToken);

        // 2. Validations
        var validationResult = ValidateTransferRules(sourceAccount, destinationAccount, request);
        if (validationResult is not null) return validationResult.Value;

        // 3. Execute & Persist
        var savedTx = await ExecuteAndPersistTransferAsync(sourceAccount!, destinationAccount!, request, cancellationToken);

        // 4. Publish Event
        await PublishTransferCompletedEventAsync(savedTx, cancellationToken);

        return Result<TransferResponseDto>.Ok(new TransferResponseDto(savedTx.Id, savedTx.Status.ToString(), savedTx.CreatedAt));
    }

    private Result<TransferResponseDto>? ValidateTransferRules(Account? source, Account? destination, TransferCommand request)
    {
        if (source is null)
        {
            _logger.LogError("Transfer failed: Source account not found. {Account}", request.SourceAccountNumber);
            return Result<TransferResponseDto>.Fail("Source account not found.");
        }

        if (destination is null)
        {
            _logger.LogError("Transfer failed: Destination account not found. {Account}", request.DestinationAccountNumber);
            return Result<TransferResponseDto>.Fail("Destination account not found.");
        }

        if (!source.IsActive())
            return Result<TransferResponseDto>.Fail("Source account is not active.");

        if (!destination.IsActive())
            return Result<TransferResponseDto>.Fail("Destination account is not active.");

        if (!source.CanDebit(request.Amount))
        {
            _logger.LogError("Transfer failed: Insufficient funds in {Account}.", request.SourceAccountNumber);
            return Result<TransferResponseDto>.Fail("Insufficient funds in source account.");
        }

        return null;
    }

    private async Task<Transaction> ExecuteAndPersistTransferAsync(Account source, Account destination, TransferCommand request, CancellationToken cancellationToken)
    {
        source.Balance -= request.Amount;
        destination.Balance += request.Amount;

        await _accountRepository.SaveAsync(source, cancellationToken);
        await _accountRepository.SaveAsync(destination, cancellationToken);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = request.SourceAccountNumber,
            DestinationAccountNumber = request.DestinationAccountNumber,
            Amount = request.Amount,
            Currency = request.Currency,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        return await _transactionRepository.SaveAsync(transaction, cancellationToken);
    }

    private async Task PublishTransferCompletedEventAsync(Transaction tx, CancellationToken cancellationToken)
    {
        var ev = new TransferCompleted(tx.Id, tx.SourceAccountNumber, tx.DestinationAccountNumber, tx.Amount, tx.Currency.ToString(), tx.IdempotencyKey, tx.Status.ToString(), tx.CreatedAt);
        await _publisher.PublishAsync("transfers-topic", ev, cancellationToken);
    }
}

