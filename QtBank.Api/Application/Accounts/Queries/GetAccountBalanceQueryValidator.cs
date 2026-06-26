using FluentValidation;

namespace QtBank.Api.Application.Accounts.Queries;

public class GetAccountBalanceQueryValidator : AbstractValidator<GetAccountBalanceQuery>
{
    public GetAccountBalanceQueryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number cannot be empty.");
    }
}
