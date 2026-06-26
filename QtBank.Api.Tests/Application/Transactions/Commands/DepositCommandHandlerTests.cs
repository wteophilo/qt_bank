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

public class DepositCommandHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<DepositCommandHandler> _logger;
    private readonly DepositCommandHandler _handler;

    public DepositCommandHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _publisher = Substitute.For<IPubSubPublisher>();
        _logger = Substitute.For<ILogger<DepositCommandHandler>>();
        _handler = new DepositCommandHandler(
            _accountRepository,
            _transactionRepository,
            _publisher,
            _logger
        );
    }

    [Fact]
    public async Task Handle_ShouldExecuteDepositAndPublishEvent_WhenCommandIsValid()
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

        var command = new DepositCommand(accountNumber, 300m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Processing");
        result.Value.TransactionId.Should().NotBeEmpty();

        account.Balance.Should().Be(1300m);

        // Verify account saved with updated balance
        await _accountRepository.Received(1).SaveAsync(account, Arg.Any<CancellationToken>());

        // Verify transaction saved
        await _transactionRepository.Received(1).SaveAsync(
            Arg.Is<Transaction>(t =>
                t.SourceAccountNumber == string.Empty &&
                t.DestinationAccountNumber == accountNumber &&
                t.Amount == 300m &&
                t.Currency == Currency.USD &&
                t.Type == TransactionType.Deposit &&
                t.IdempotencyKey != Guid.Empty &&
                t.Status == TransactionStatus.Processing
            ),
            Arg.Any<CancellationToken>()
        );

        // Verify event published
        await _publisher.Received(1).PublishAsync(
            "deposits-topic",
            Arg.Is<DepositCompleted>(e =>
                e.TransactionId == result.Value.TransactionId &&
                e.AccountNumber == accountNumber &&
                e.Amount == 300m &&
                e.Currency == "USD" &&
                e.IdempotencyKey != Guid.Empty &&
                e.Status == "Processing"
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

        var command = new DepositCommand(accountNumber, 100m, Currency.USD);

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
            Status = AccountStatus.Inactive 
        };

        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new DepositCommand(accountNumber, 100m, Currency.USD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is not active.");
    }
}
