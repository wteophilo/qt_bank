using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QtBank.Api.Application.Accounts.Commands;
using QtBank.Api.Domain.Events;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;
using Xunit;

namespace QtBank.Api.Tests.Application.Accounts.Commands;

public class CreateAccountCommandHandlerTests
{
    private readonly IAccountRepository _repository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<CreateAccountCommandHandler> _logger;
    private readonly CreateAccountCommandHandler _handler;

    public CreateAccountCommandHandlerTests()
    {
        _repository = Substitute.For<IAccountRepository>();
        _publisher = Substitute.For<IPubSubPublisher>();
        _logger = Substitute.For<ILogger<CreateAccountCommandHandler>>();
        _handler = new CreateAccountCommandHandler(_repository, _publisher, _logger);
    }

    [Fact]
    public async Task Handle_ShouldSaveAccountAndPublishEvent_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateAccountCommand(" 12345-6 ", 1500.75m, " John Doe ", AccountStatus.Active);
        
        // Configure repository stub to return the account it receives
        _repository.SaveAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Account>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.AccountNumber.Should().Be("12345-6"); // Trimming verified
        result.Balance.Should().Be(1500.75m);
        result.OwnerName.Should().Be("John Doe"); // Trimming verified
        result.Status.Should().Be("Active");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Verify repository saved the exact details
        await _repository.Received(1).SaveAsync(
            Arg.Is<Account>(a => 
                a.AccountNumber == "12345-6" &&
                a.OwnerName == "John Doe" &&
                a.Balance == 1500.75m &&
                a.Status == AccountStatus.Active
            ), 
            Arg.Any<CancellationToken>()
        );

        // Verify event was published correctly
        await _publisher.Received(1).PublishAsync(
            "accounts-topic",
            Arg.Is<AccountCreated>(e => 
                e.AccountId == result.Id &&
                e.AccountNumber == "12345-6" &&
                e.Balance == 1500.75m &&
                e.OwnerName == "John Doe" &&
                e.Status == AccountStatus.Active
            ),
            Arg.Any<CancellationToken>()
        );
    }
}
