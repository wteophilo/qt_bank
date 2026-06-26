using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Application.Transactions.Queries;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Queries;

public class GetAccountTransactionsQueryHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<GetAccountTransactionsQueryHandler> _logger;
    private readonly GetAccountTransactionsQueryHandler _handler;

    public GetAccountTransactionsQueryHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _logger = Substitute.For<ILogger<GetAccountTransactionsQueryHandler>>();
        _handler = new GetAccountTransactionsQueryHandler(_accountRepository, _transactionRepository, _logger);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountNumber = "999999";
        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var query = new GetAccountTransactionsQuery(accountNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await _accountRepository.Received(1).GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>());
        await _transactionRepository.DidNotReceiveWithAnyArgs().GetByAccountNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedTransactions_WhenAccountExists()
    {
        // Arrange
        var accountNumber = "111111";
        var account = new Account { Id = Guid.NewGuid(), AccountNumber = accountNumber, Status = AccountStatus.Active };
        
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceAccountNumber = accountNumber,
                DestinationAccountNumber = "222222",
                Amount = 100m,
                Currency = Currency.USD,
                Type = TransactionType.Transfer,
                IdempotencyKey = Guid.NewGuid(),
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceAccountNumber = "333333",
                DestinationAccountNumber = accountNumber,
                Amount = 250m,
                Currency = Currency.BRL,
                Type = TransactionType.Deposit,
                IdempotencyKey = Guid.NewGuid(),
                Status = TransactionStatus.Processing,
                CreatedAt = DateTime.UtcNow
            }
        };

        _accountRepository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>()).Returns(account);
        _transactionRepository.GetByAccountNumberAsync(accountNumber, Arg.Any<CancellationToken>()).Returns(transactions);

        var query = new GetAccountTransactionsQuery(accountNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var resultList = result!.ToList();
        resultList.Should().HaveCount(2);

        resultList[0].SourceAccountNumber.Should().Be(accountNumber);
        resultList[0].DestinationAccountNumber.Should().Be("222222");
        resultList[0].Amount.Should().Be(100m);
        resultList[0].Currency.Should().Be("USD");
        resultList[0].Type.Should().Be("Transfer");
        resultList[0].Status.Should().Be("Completed");

        resultList[1].SourceAccountNumber.Should().Be("333333");
        resultList[1].DestinationAccountNumber.Should().Be(accountNumber);
        resultList[1].Amount.Should().Be(250m);
        resultList[1].Currency.Should().Be("BRL");
        resultList[1].Type.Should().Be("Deposit");
        resultList[1].Status.Should().Be("Processing");

        await _accountRepository.Received(1).GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).GetByAccountNumberAsync(accountNumber, Arg.Any<CancellationToken>());
    }
}
