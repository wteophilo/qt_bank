using FluentAssertions;
using QtBank.Api.Application.Accounts.Commands;
using QtBank.Api.Domain.Models;
using Xunit;

namespace QtBank.Api.Tests.Application.Accounts.Commands;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _validator;

    public CreateAccountValidatorTests()
    {
        _validator = new CreateAccountValidator();
    }

    [Fact]
    public void Validator_ShouldBeValid_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new CreateAccountCommand("123-456", 100.00m, "Alice Smith", AccountStatus.Active);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validator_ShouldFail_WhenAccountNumberIsEmptyOrNull(string? invalidAccountNumber)
    {
        // Arrange
        var command = new CreateAccountCommand(invalidAccountNumber!, 100.00m, "Alice Smith", AccountStatus.Active);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => 
            error.PropertyName == nameof(CreateAccountCommand.AccountNumber) && 
            error.ErrorMessage == "Account number cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validator_ShouldFail_WhenOwnerNameIsEmptyOrNull(string? invalidOwnerName)
    {
        // Arrange
        var command = new CreateAccountCommand("123-456", 100.00m, invalidOwnerName!, AccountStatus.Active);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => 
            error.PropertyName == nameof(CreateAccountCommand.OwnerName) && 
            error.ErrorMessage == "Owner name cannot be empty.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenBalanceIsNegative()
    {
        // Arrange
        var command = new CreateAccountCommand("123-456", -0.01m, "Alice Smith", AccountStatus.Active);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => 
            error.PropertyName == nameof(CreateAccountCommand.Balance) && 
            error.ErrorMessage == "Initial balance cannot be negative.");
    }
}
