using System;
using System.Linq;
using FluentValidation;

namespace QtBank.Api.Application.Transactions.Commands;

/// <summary>
/// Validator for TransferCommand to enforce basic request constraints.
/// </summary>
public class TransferValidator : AbstractValidator<TransferCommand>
{
    private static readonly string[] SupportedCurrencies = ["BRL", "USD", "EUR"];

    public TransferValidator()
    {
        RuleFor(x => x.SourceAccountNumber)
            .NotEmpty().WithMessage("Source account number is required.")
            .Matches("^[0-9]{6}$").WithMessage("Account number must contain only digits.");

        RuleFor(x => x.DestinationAccountNumber)
            .NotEmpty().WithMessage("Destination account number is required.")
            .Matches("^[0-9]{6}$").WithMessage("Account number must contain only digits.")
            .NotEqual(x => x.SourceAccountNumber).WithMessage("Source and destination accounts cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(currency => SupportedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Currency must be one of the supported types: {string.Join(", ", SupportedCurrencies)}.");
    }
}
