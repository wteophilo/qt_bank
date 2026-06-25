using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using QtBank.Api.Application.Behaviors;
using Xunit;

namespace QtBank.Api.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest(string Data) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ShouldCallNextDelegate()
    {
        // Arrange
        var request = new TestRequest("some-data");
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("success-response");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("success-response");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidatorsPass_ShouldCallNextDelegate()
    {
        // Arrange
        var request = new TestRequest("valid-data");
        
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult()); // No errors

        var validator2 = Substitute.For<IValidator<TestRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult()); // No errors

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("success-response");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("success-response");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidatorsFail_ShouldThrowValidationException_AndNotCallNextDelegate()
    {
        // Arrange
        var request = new TestRequest("invalid-data");

        var failure1 = new ValidationFailure("Data", "Data must not be invalid.");
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure1 }));

        var failure2 = new ValidationFailure("Data", "Data is too short.");
        var validator2 = Substitute.For<IValidator<TestRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure2 }));

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("success-response");
        };

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
        exceptionAssertion.Which.Errors.Should().HaveCount(2);
        exceptionAssertion.Which.Errors.Select(e => e.ErrorMessage)
            .Should().ContainInOrder("Data must not be invalid.", "Data is too short.");

        nextCalled.Should().BeFalse();
    }
}
