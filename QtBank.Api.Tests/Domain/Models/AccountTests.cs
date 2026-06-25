using System;
using FluentAssertions;
using QtBank.Api.Domain.Models;
using Xunit;

namespace QtBank.Api.Tests.Domain.Models;

public class AccountTests
{
    [Fact]
    public void Account_ShouldInitializeWithDefaultValues()
    {
        // Act
        var account = new Account();

        // Assert
        account.Id.Should().BeEmpty();
        account.AccountNumber.Should().BeEmpty();
        account.Balance.Should().Be(0m);
        account.OwnerName.Should().BeEmpty();
        account.CreatedAt.Should().Be(default);
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Account_ShouldAllowSettingProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var accountNumber = "12345-6";
        var balance = 1500.50m;
        var ownerName = "John Doe";
        var createdAt = DateTime.UtcNow;
        var status = AccountStatus.Inactive;

        // Act
        var account = new Account
        {
            Id = id,
            AccountNumber = accountNumber,
            Balance = balance,
            OwnerName = ownerName,
            CreatedAt = createdAt,
            Status = status
        };

        // Assert
        account.Id.Should().Be(id);
        account.AccountNumber.Should().Be(accountNumber);
        account.Balance.Should().Be(balance);
        account.OwnerName.Should().Be(ownerName);
        account.CreatedAt.Should().Be(createdAt);
        account.Status.Should().Be(status);
    }
}
