using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QtBank.Api.Infrastructure.Middlewares;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Middlewares;

public class ValidationExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextDelegateCompletesSuccessfully_ShouldNotModifyResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        RequestDelegate next = (ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new ValidationExceptionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        responseBodyStream.Length.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionIsThrown_ShouldSetStatus400AndReturnProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var failures = new List<ValidationFailure>
        {
            new("AccountNumber", "Account number cannot be empty."),
            new("OwnerName", "Owner name is required."),
            new("OwnerName", "Owner name is too short.")
        };
        var validationException = new ValidationException(failures);

        RequestDelegate next = (ctx) => throw validationException;

        var middleware = new ValidationExceptionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/json");

        // Parse Response Body
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream);
        var responseJson = await reader.ReadToEndAsync();
        responseJson.Should().NotBeNullOrWhiteSpace();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Detail.Should().Be("Please refer to the errors property for additional details.");
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");

        // Verify extensions contain mapped validation errors
        problemDetails.Extensions.Should().ContainKey("errors");
        var errorsJson = problemDetails.Extensions["errors"]?.ToString();
        errorsJson.Should().NotBeNull();

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson!);
        errors.Should().NotBeNull();
        errors.Should().HaveCount(2);

        errors!["AccountNumber"].Should().ContainSingle().Which.Should().Be("Account number cannot be empty.");
        errors["OwnerName"].Should().HaveCount(2)
            .And.ContainInOrder("Owner name is required.", "Owner name is too short.");
    }

    [Fact]
    public async Task InvokeAsync_WhenOtherExceptionIsThrown_ShouldBubbleUpException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Some unexpected error occurred.");
        
        RequestDelegate next = (ctx) => throw expectedException;

        var middleware = new ValidationExceptionMiddleware(next);

        // Act
        Func<Task> act = async () => await middleware.InvokeAsync(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Some unexpected error occurred.");
    }
}
