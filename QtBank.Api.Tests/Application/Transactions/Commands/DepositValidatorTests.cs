using System;
using FluentAssertions;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Domain.Models;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Commands;

public class DepositValidatorTests
{
    private readonly DepositValidator _validator;

    public DepositValidatorTests()
    {
        _validator = new DepositValidator();
    }

    [Fact]
    public void Validator_ShouldBeValid_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new DepositCommand(
            "111111",
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validator_ShouldFail_WhenAccountNumberIsEmpty()
    {
        // Arrange
        var command = new DepositCommand(
            "",
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(DepositCommand.AccountNumber) &&
            error.ErrorMessage == "Account number is required.");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12a456")]
    public void Validator_ShouldFail_WhenAccountNumberIsInvalid(string invalidAccountNumber)
    {
        // Arrange
        var command = new DepositCommand(
            invalidAccountNumber,
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(DepositCommand.AccountNumber) &&
            error.ErrorMessage == "Account number must contain only digits.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validator_ShouldFail_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        // Arrange
        var command = new DepositCommand(
            "111111",
            invalidAmount,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(DepositCommand.Amount) &&
            error.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenCurrencyIsUnsupported()
    {
        // Arrange
        var command = new DepositCommand(
            "111111",
            100m,
            (Currency)999 // Unsupported
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(DepositCommand.Currency) &&
            error.ErrorMessage == "Currency must be one of the supported types: BRL, USD, EUR, CAD.");
    }
}
