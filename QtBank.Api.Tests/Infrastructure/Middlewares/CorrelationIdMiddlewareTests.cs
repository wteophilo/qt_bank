using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QtBank.Api.Infrastructure.Middlewares;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Middlewares;

/// <summary>
/// Unit tests for <see cref="CorrelationIdMiddleware"/>.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly IOptions<CorrelationIdOptions> _options;

    public CorrelationIdMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<CorrelationIdMiddleware>>();
        _options = Options.Create(new CorrelationIdOptions());
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasCorrelationIdHeader_ShouldUseExistingHeaderValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = expectedCorrelationId;

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items["CorrelationId"].Should().Be(expectedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestLacksCorrelationIdHeader_ShouldGenerateNewGuid()
    {
        // Arrange
        var context = new DefaultHttpContext();

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var resultCorrelationIdObj = context.Items["CorrelationId"];
        resultCorrelationIdObj.Should().NotBeNull();
        
        var resultCorrelationId = resultCorrelationIdObj!.ToString();
        Guid.TryParse(resultCorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAppendHeaderToResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var expectedCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = expectedCorrelationId;

        RequestDelegate next = async (ctx) =>
        {
            // Writing to the body forces the response to start, which triggers OnStarting callbacks
            await ctx.Response.WriteAsync("Test response");
        };

        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be(expectedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldEnrichLoggingScope()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = expectedCorrelationId;

        bool scopeCalled = false;

        // Custom check to verify that ILogger.BeginScope is called with the expected Correlation ID
        _logger.When(l => l.BeginScope(Arg.Any<object>()))
               .Do(info =>
               {
                   var scopeObj = info.Arg<object>();
                   if (scopeObj is IEnumerable<KeyValuePair<string, object>> dictState)
                   {
                       foreach (var kvp in dictState)
                       {
                           if (kvp.Key == "CorrelationId" && kvp.Value.ToString() == expectedCorrelationId)
                           {
                               scopeCalled = true;
                           }
                       }
                   }
               });

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        scopeCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasAuthToken_ShouldExtractSessionIdAndAddToLoggerScope()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = correlationId;

        // Generate a token which now contains the SessionId
        var token = QtBank.Api.Infrastructure.Security.TokenGenerator.GenerateToken("test-user");
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        bool sessionIdInScope = false;

        _logger.When(l => l.BeginScope(Arg.Any<object>()))
               .Do(info =>
               {
                   var scopeObj = info.Arg<object>();
                   if (scopeObj is IEnumerable<KeyValuePair<string, object>> dictState)
                   {
                       foreach (var kvp in dictState)
                       {
                           if (kvp.Key == "SessionId" && !string.IsNullOrWhiteSpace(kvp.Value?.ToString()))
                           {
                               sessionIdInScope = true;
                           }
                       }
                   }
               });

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.Should().ContainKey("SessionId");
        Guid.TryParse(context.Items["SessionId"]!.ToString(), out _).Should().BeTrue();
        sessionIdInScope.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenActivityIsActive_ShouldSetCorrelationIdAndSessionIdTagsOnActivity()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = expectedCorrelationId;

        // Generate a token which contains the SessionId
        var token = QtBank.Api.Infrastructure.Security.TokenGenerator.GenerateToken("test-user");
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        // Configure and start an Activity Source and Listener to simulate OpenTelemetry tracing
        using var activitySource = new ActivitySource("TestActivitySource");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        activity.Should().NotBeNull();

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Verify the correlation.id tag is set correctly
        activity!.GetTagItem("correlation.id").Should().Be(expectedCorrelationId);
        
        // Verify the session.id tag is set correctly and matches the extracted SessionId
        var expectedSessionId = context.Items["SessionId"]?.ToString();
        expectedSessionId.Should().NotBeNullOrWhiteSpace();
        activity.GetTagItem("session.id").Should().Be(expectedSessionId);
    }
}
