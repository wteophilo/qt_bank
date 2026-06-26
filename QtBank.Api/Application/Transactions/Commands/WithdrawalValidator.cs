using FluentValidation;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Validator for WithdrawalCommand to enforce basic request constraints.
/// </summary>
public class WithdrawalValidator : AbstractValidator<WithdrawalCommand>
{
    public WithdrawalValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .Matches("^[0-9]{6}$").WithMessage("Account number must contain only digits.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage(x => $"Currency must be one of the supported types: {string.Join(", ", Enum.GetNames<Currency>())}.");
    }
}
