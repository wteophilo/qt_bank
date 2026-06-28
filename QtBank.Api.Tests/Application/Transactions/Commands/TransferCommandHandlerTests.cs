using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Domain.Events;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Commands;

public class TransferCommandHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<TransferCommandHandler> _logger;
    private readonly TransferCommandHandler _handler;

    public TransferCommandHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _publisher = Substitute.For<IPubSubPublisher>();
        _logger = Substitute.For<ILogger<TransferCommandHandler>>();
        _handler = new TransferCommandHandler(
            _accountRepository,
            _transactionRepository,
            _publisher,
            _logger
        );
    }

    [Fact]
    public async Task Handle_ShouldExecuteTransferAndPublishEvent_WhenCommandIsValid()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var sourceAccount = new Account
        {
            Id = sourceId,
            AccountNumber = sourceAccountNumber,
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var destAccount = new Account
        {
            Id = destId,
            AccountNumber = destAccountNumber,
            Balance = 500m,
            Status = AccountStatus.Active
        };

        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>())
            .Returns(sourceAccount);

        _accountRepository.GetByNumberAsync(destAccountNumber, Arg.Any<CancellationToken>())
            .Returns(destAccount);

        _transactionRepository.SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Transaction>());

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 300m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Completed");
        result.Value.TransactionId.Should().NotBeEmpty();

        sourceAccount.Balance.Should().Be(700m);
        destAccount.Balance.Should().Be(800m);

        // Verify accounts saved
        await _accountRepository.Received(1).SaveAsync(sourceAccount, Arg.Any<CancellationToken>());
        await _accountRepository.Received(1).SaveAsync(destAccount, Arg.Any<CancellationToken>());

        // Verify transaction saved
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(t =>
                t.SourceAccountNumber == sourceAccountNumber &&
                t.DestinationAccountNumber == destAccountNumber &&
                t.Amount == 300m &&
                t.Currency == Currency.USD &&
                t.Type == TransactionType.Transfer &&
                t.IdempotencyKey != Guid.Empty &&
                t.Status == TransactionStatus.Completed
            ),
            Arg.Any<CancellationToken>()
        );

        // Verify event published
        await _publisher.Received(1).PublishAsync(
            "transfers-topic",
            Arg.Is<TransferCompleted>(e =>
                e.TransactionId == result.Value.TransactionId &&
                e.SourceAccountNumber == sourceAccountNumber &&
                e.DestinationAccountNumber == destAccountNumber &&
                e.Amount == 300m &&
                e.Currency == "USD" &&
                e.IdempotencyKey != Guid.Empty &&
                e.Status == "Completed"
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenSourceAccountNotFound()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Source account not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenDestinationAccountNotFound()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        var sourceAccount = new Account { Id = Guid.NewGuid(), AccountNumber = sourceAccountNumber, Status = AccountStatus.Active };

        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>()).Returns(sourceAccount);
        _accountRepository.GetByNumberAsync(destAccountNumber, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Destination account not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenSourceAccountIsNotActive()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        var sourceAccount = new Account { Id = Guid.NewGuid(), AccountNumber = sourceAccountNumber, Status = AccountStatus.Inactive };
        var destAccount = new Account { Id = Guid.NewGuid(), AccountNumber = destAccountNumber, Status = AccountStatus.Active };

        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>()).Returns(sourceAccount);
        _accountRepository.GetByNumberAsync(destAccountNumber, Arg.Any<CancellationToken>()).Returns(destAccount);

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Source account is not active.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenDestinationAccountIsNotActive()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        var sourceAccount = new Account { Id = Guid.NewGuid(), AccountNumber = sourceAccountNumber, Status = AccountStatus.Active };
        var destAccount = new Account { Id = Guid.NewGuid(), AccountNumber = destAccountNumber, Status = AccountStatus.Inactive };

        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>()).Returns(sourceAccount);
        _accountRepository.GetByNumberAsync(destAccountNumber, Arg.Any<CancellationToken>()).Returns(destAccount);

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Destination account is not active.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenInsufficientFunds()
    {
        // Arrange
        var sourceAccountNumber = "111111";
        var destAccountNumber = "222222";
        var sourceAccount = new Account { Id = Guid.NewGuid(), AccountNumber = sourceAccountNumber, Balance = 50m, Status = AccountStatus.Active };
        var destAccount = new Account { Id = Guid.NewGuid(), AccountNumber = destAccountNumber, Balance = 200m, Status = AccountStatus.Active };

        _accountRepository.GetByNumberAsync(sourceAccountNumber, Arg.Any<CancellationToken>()).Returns(sourceAccount);
        _accountRepository.GetByNumberAsync(destAccountNumber, Arg.Any<CancellationToken>()).Returns(destAccount);

        var command = new TransferCommand(sourceAccountNumber, destAccountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Insufficient funds in source account.");
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedResult_WhenIdempotencyKeyAlreadyExists()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var existingTransactionId = Guid.NewGuid();
        var existingTx = new Transaction
        {
            Id = existingTransactionId,
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            Amount = 300m,
            Currency = Currency.USD,
            Type = TransactionType.Transfer,
            IdempotencyKey = idempotencyKey,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existingTx);

        var command = new TransferCommand("111111", "222222", 300m, Currency.USD, idempotencyKey);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TransactionId.Should().Be(existingTransactionId);
        result.Value.Status.Should().Be("Completed");
        result.Value.Timestamp.Should().Be(existingTx.CreatedAt);

        // Verify repository save and publisher were not called
        await _accountRepository.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
