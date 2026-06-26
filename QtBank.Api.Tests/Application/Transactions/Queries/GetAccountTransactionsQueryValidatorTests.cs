using FluentAssertions;
using QtBank.Api.Application.Transactions.Queries;
using Xunit;

namespace QtBank.Api.Tests.Application.Transactions.Queries;

public class GetAccountTransactionsQueryValidatorTests
{
    private readonly GetAccountTransactionsQueryValidator _validator;

    public GetAccountTransactionsQueryValidatorTests()
    {
        _validator = new GetAccountTransactionsQueryValidator();
    }

    [Fact]
    public void Validator_ShouldBeValid_WhenAccountNumberIsNotEmpty()
    {
        // Arrange
        var query = new GetAccountTransactionsQuery("123456");

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
        var query = new GetAccountTransactionsQuery(invalidAccountNumber!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(GetAccountTransactionsQuery.AccountNumber) &&
            error.ErrorMessage == "Account number cannot be empty.");
    }
}
