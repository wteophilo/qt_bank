using FluentValidation;

namespace QtBank.Api.Application.Accounts.Commands;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number cannot be empty.");

        RuleFor(x => x.OwnerName)
            .NotEmpty().WithMessage("Owner name cannot be empty.");

        RuleFor(x => x.Balance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance cannot be negative.");
    }
}
