using FluentValidation;
using MediatR;
using QtBank.Api.Common.Security;

namespace QtBank.Api.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, string Email);

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly ITokenGenerator _tokenGenerator;

    public LoginCommandHandler(ITokenGenerator tokenGenerator)
    {
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Simulate database lookup / password verification (this is a boilerplate!)
        await Task.Delay(50, cancellationToken);

        if (request.Email == "admin@qtbank.com" && request.Password == "password123")
        {
            var token = _tokenGenerator.GenerateToken("1", request.Email, "Administrator");
            return new LoginResult(token, request.Email);
        }

        if (request.Email == "user@qtbank.com" && request.Password == "password123")
        {
            var token = _tokenGenerator.GenerateToken("2", request.Email, "User");
            return new LoginResult(token, request.Email);
        }

        throw new ValidationException(new[]
        {
            new FluentValidation.Results.ValidationFailure("Email", "Invalid email or password.")
        });
    }
}
