using System;
using FluentAssertions;
using QtBank.Api.Domain.Exceptions;
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

    [Theory]
    [InlineData(AccountStatus.Active, true)]
    [InlineData(AccountStatus.Inactive, false)]
    public void IsActive_ShouldReturnExpectedResult_BasedOnStatus(AccountStatus status, bool expectedResult)
    {
        // Arrange
        var account = new Account { Status = status };

        // Act
        var isActive = account.IsActive();

        // Assert
        isActive.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AccountStatus.Active, 100, 50, true)]   // Active, sufficient balance
    [InlineData(AccountStatus.Active, 100, 100, true)]  // Active, exact balance
    [InlineData(AccountStatus.Active, 100, 150, false)] // Active, insufficient balance
    [InlineData(AccountStatus.Inactive, 100, 50, false)] // Inactive, sufficient balance
    public void CanDebit_ShouldReturnExpectedResult_BasedOnStatusAndBalance(
        AccountStatus status,
        decimal initialBalance,
        decimal debitAmount,
        bool expectedResult)
    {
        // Arrange
        var account = new Account { Status = status, Balance = initialBalance };

        // Act
        var canDebit = account.CanDebit(debitAmount);

        // Assert
        canDebit.Should().Be(expectedResult);
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance_WhenCanDebitIsTrue()
    {
        // Arrange
        var account = new Account { Status = AccountStatus.Active, Balance = 200m };

        // Act
        account.Debit(50m);

        // Assert
        account.Balance.Should().Be(150m);
    }

    [Fact]
    public void Debit_ShouldThrowDomainException_WhenCanDebitIsFalse()
    {
        // Arrange
        var account = new Account { Status = AccountStatus.Inactive, Balance = 200m };

        // Act
        Action action = () => account.Debit(50m);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot debit account.");
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance_WhenAccountIsActive()
    {
        // Arrange
        var account = new Account { Status = AccountStatus.Active, Balance = 200m };

        // Act
        account.Credit(50m);

        // Assert
        account.Balance.Should().Be(250m);
    }

    [Fact]
    public void Credit_ShouldThrowDomainException_WhenAccountIsInactive()
    {
        // Arrange
        var account = new Account { Status = AccountStatus.Inactive, Balance = 200m };

        // Act
        Action action = () => account.Credit(50m);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot credit inactive account.");
    }
}
