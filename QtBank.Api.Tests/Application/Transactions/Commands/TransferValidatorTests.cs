using System;
using FluentAssertions;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Domain.Models;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Commands;

public class TransferValidatorTests
{
    private readonly TransferValidator _validator;

    public TransferValidatorTests()
    {
        _validator = new TransferValidator();
    }

    [Fact]
    public void Validator_ShouldBeValid_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new TransferCommand(
            "111111",
            "222222",
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
    public void Validator_ShouldFail_WhenSourceAccountNumberIsEmpty()
    {
        // Arrange
        var command = new TransferCommand(
            "",
            "222222",
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.SourceAccountNumber) &&
            error.ErrorMessage == "Source account number is required.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenDestinationAccountNumberIsEmpty()
    {
        // Arrange
        var command = new TransferCommand(
            "111111",
            "",
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.DestinationAccountNumber) &&
            error.ErrorMessage == "Destination account number is required.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenSourceAndDestinationAreSame()
    {
        // Arrange
        var accountNo = "111111";
        var command = new TransferCommand(
            accountNo,
            accountNo,
            150.00m,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.DestinationAccountNumber) &&
            error.ErrorMessage == "Source and destination accounts cannot be the same.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validator_ShouldFail_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        // Arrange
        var command = new TransferCommand(
            "111111",
            "222222",
            invalidAmount,
            Currency.USD
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.Amount) &&
            error.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenCurrencyIsUnsupported()
    {
        // Arrange
        var command = new TransferCommand(
            "111111",
            "222222",
            100m,
            (Currency)999 // Unsupported
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.Currency) &&
            error.ErrorMessage == "Currency must be one of the supported types: BRL, USD, EUR, CAD.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenIdempotencyKeyIsEmpty()
    {
        // Arrange
        var command = new TransferCommand(
            "111111",
            "222222",
            150.00m,
            Currency.USD,
            Guid.Empty
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(TransferCommand.IdempotencyKey) &&
            error.ErrorMessage == "Idempotency key is required.");
    }
}
