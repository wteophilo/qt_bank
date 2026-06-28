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
using QtBank.Api.Infrastructure.Telemetry;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Commands;

public class WithdrawalCommandHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ApplicationMetrics _metrics;
    private readonly ILogger<WithdrawalCommandHandler> _logger;
    private readonly WithdrawalCommandHandler _handler;

    public WithdrawalCommandHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _metrics = new ApplicationMetrics();
        _logger = Substitute.For<ILogger<WithdrawalCommandHandler>>();
        _handler = new WithdrawalCommandHandler(
            _accountRepository,
            _transactionRepository,
            _outboxRepository,
            _metrics,
            _logger
        );
    }

    [Fact]
    public async Task Handle_ShouldExecuteWithdrawalAndPublishEvent_WhenCommandIsValid()
    {
        // Arrange
        var accountNumber = "111111";
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            AccountNumber = accountNumber,
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns(account);

        _transactionRepository.SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Transaction>());

        var command = new WithdrawalCommand(accountNumber, 300m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Completed");
        result.Value.TransactionId.Should().NotBeEmpty();

        account.Balance.Should().Be(700m);

        // Verify account saved with updated balance
        await _accountRepository.Received(1).SaveAsync(account, Arg.Any<CancellationToken>());

        // Verify transaction saved
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(t =>
                t.SourceAccountNumber == accountNumber &&
                t.DestinationAccountNumber == string.Empty &&
                t.Amount == 300m &&
                t.Currency == Currency.USD &&
                t.Type == TransactionType.Withdrawal &&
                t.IdempotencyKey != Guid.Empty &&
                t.Status == TransactionStatus.Completed
            ),
            Arg.Any<CancellationToken>()
        );

        // Verify outbox message saved
        await _outboxRepository.Received(1).SaveAsync(
            Arg.Is<OutboxMessage>(m =>
                m.Topic == "withdrawals-topic" &&
                m.Type.Contains("WithdrawalCompleted") &&
                m.Content.Contains(accountNumber) &&
                m.Content.Contains("300")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenAccountNotFound()
    {
        // Arrange
        var accountNumber = "111111";
        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var command = new WithdrawalCommand(accountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenAccountIsNotActive()
    {
        // Arrange
        var accountNumber = "111111";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Status = AccountStatus.Inactive,
            Balance = 500m
        };

        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new WithdrawalCommand(accountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is not active.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenInsufficientFunds()
    {
        // Arrange
        var accountNumber = "111111";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Status = AccountStatus.Active,
            Balance = 50m
        };

        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new WithdrawalCommand(accountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Insufficient funds.");
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
            DestinationAccountNumber = string.Empty,
            Amount = 300m,
            Currency = Currency.USD,
            Type = TransactionType.Withdrawal,
            IdempotencyKey = idempotencyKey,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existingTx);

        var command = new WithdrawalCommand("111111", 300m, Currency.USD, idempotencyKey);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TransactionId.Should().Be(existingTransactionId);
        result.Value.Status.Should().Be("Completed");
        result.Value.Timestamp.Should().Be(existingTx.CreatedAt);

        // Verify repository save and outbox were not called
        await _accountRepository.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _transactionRepository.DidNotReceive().SaveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }
}
