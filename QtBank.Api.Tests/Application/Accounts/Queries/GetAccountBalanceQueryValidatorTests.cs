using FluentAssertions;
using QtBank.Api.Application.Accounts.Queries;
using Xunit;

namespace QtBank.Api.Tests.Application.Accounts.Queries;

public class GetAccountBalanceQueryValidatorTests
{
    private readonly GetAccountBalanceQueryValidator _validator;

    public GetAccountBalanceQueryValidatorTests()
    {
        _validator = new GetAccountBalanceQueryValidator();
    }

    [Fact]
    public void Validator_ShouldBeValid_WhenAccountNumberIsValid()
    {
        // Arrange
        var query = new GetAccountBalanceQuery("123456");

        // Act
        var result = _validator.Validate(query);

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
        var query = new GetAccountBalanceQuery(invalidAccountNumber!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(GetAccountBalanceQuery.AccountNumber) &&
            error.ErrorMessage == "Account number cannot be empty.");
    }
}
