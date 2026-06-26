using FluentValidation;

namespace QtBank.Api.Application.Transactions.Queries;

/// <summary>
/// Validator for GetAccountTransactionsQuery to enforce basic input constraints.
/// </summary>
public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
{
    public GetAccountTransactionsQueryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number cannot be empty.");
    }
}
