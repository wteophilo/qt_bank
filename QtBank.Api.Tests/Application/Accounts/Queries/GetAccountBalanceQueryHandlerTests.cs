using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QtBank.Api.Application.Accounts.Queries;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using Xunit;

namespace QtBank.Api.Tests.Application.Accounts.Queries;

public class GetAccountBalanceQueryHandlerTests
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountBalanceQueryHandler> _logger;
    private readonly GetAccountBalanceQueryHandler _handler;

    public GetAccountBalanceQueryHandlerTests()
    {
        _repository = Substitute.For<IAccountRepository>();
        _logger = Substitute.For<ILogger<GetAccountBalanceQueryHandler>>();
        _handler = new GetAccountBalanceQueryHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_ShouldReturnAccountBalanceResponse_WhenAccountExists()
    {
        // Arrange
        var accountNumber = "123456";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            Balance = 1500.50m,
            OwnerName = "John Doe",
            CreatedAt = DateTime.UtcNow,
            Status = AccountStatus.Active
        };

        _repository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery(accountNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AccountNumber.Should().Be(accountNumber);
        result.Balance.Should().Be(1500.50m);

        await _repository.Received(1).GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountNumber = "999999";
        _repository.GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var query = new GetAccountBalanceQuery(accountNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        await _repository.Received(1).GetByNumberAsync(accountNumber, Arg.Any<CancellationToken>());
    }
}
