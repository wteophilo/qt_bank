using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using QtBank.Api.Infrastructure.Http;
using QtBank.Api.Infrastructure.Middlewares;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Http;

/// <summary>
/// Unit tests for <see cref="CorrelationIdDelegatingHandler"/>.
/// </summary>
public class CorrelationIdDelegatingHandlerTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<CorrelationIdOptions> _options;
    private readonly CorrelationIdDelegatingHandler _handler;

    public CorrelationIdDelegatingHandlerTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _options = Options.Create(new CorrelationIdOptions());
        _handler = new CorrelationIdDelegatingHandler(_httpContextAccessor, _options)
        {
            InnerHandler = new DummyHttpMessageHandler()
        };
    }

    [Fact]
    public async Task SendAsync_WhenCorrelationIdInHttpContextItems_ShouldAddHeaderToOutgoingRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        _httpContextAccessor.HttpContext.Returns(context);

        using var client = new HttpClient(_handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.external.local/users");

        // Act
        await client.SendAsync(request);

        // Assert
        request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        request.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task SendAsync_WhenCorrelationIdInRequestHeadersOnly_ShouldFallbackAndAddHeaderToOutgoingRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = correlationId;
        _httpContextAccessor.HttpContext.Returns(context);

        using var client = new HttpClient(_handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.external.local/users");

        // Act
        await client.SendAsync(request);

        // Assert
        request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        request.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task SendAsync_WhenHttpContextIsNull_ShouldProceedWithoutAddingHeader()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);

        using var client = new HttpClient(_handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.external.local/users");

        // Act
        await client.SendAsync(request);

        // Assert
        request.Headers.Contains("X-Correlation-Id").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenOutgoingRequestAlreadyHasCorrelationId_ShouldNotOverwriteIt()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = Guid.NewGuid().ToString(); // context correlation ID
        _httpContextAccessor.HttpContext.Returns(context);

        var existingCorrelationId = Guid.NewGuid().ToString();

        using var client = new HttpClient(_handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.external.local/users");
        request.Headers.Add("X-Correlation-Id", existingCorrelationId); // already present on outgoing request

        // Act
        await client.SendAsync(request);

        // Assert
        request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        request.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(existingCorrelationId);
    }

    /// <summary>
    /// A dummy message handler to prevent real network requests and return HTTP 200 OK.
    /// </summary>
    private class DummyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
