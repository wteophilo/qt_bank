using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using QtBank.Api.Infrastructure.Middlewares;

namespace QtBank.Api.Infrastructure.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> that retrieves the Correlation ID of the current
/// HTTP request using <see cref="IHttpContextAccessor"/> and propagates it as an outgoing request header.
/// </summary>
public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CorrelationIdOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdDelegatingHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor to retrieve the current HTTP context.</param>
    /// <param name="options">The configured Correlation ID options.</param>
    public CorrelationIdDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        IOptions<CorrelationIdOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Intercepts outgoing HTTP requests to append the Correlation ID header.
    /// </summary>
    /// <param name="request">The outgoing HTTP request message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response message representing the completion of request execution.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            // 1. Try to resolve the Correlation ID from HttpContext.Items
            if (httpContext.Items.TryGetValue(_options.LogScopeKey, out var correlationIdObj) &&
                correlationIdObj is string correlationId)
            {
                if (!request.Headers.Contains(_options.HeaderName))
                {
                    request.Headers.Add(_options.HeaderName, correlationId);
                }
            }
            // 2. Fallback to extracting it directly from the inbound request headers
            else if (httpContext.Request.Headers.TryGetValue(_options.HeaderName, out var inboundHeaderValue) &&
                     !string.IsNullOrWhiteSpace(inboundHeaderValue))
            {
                if (!request.Headers.Contains(_options.HeaderName))
                {
                    request.Headers.Add(_options.HeaderName, inboundHeaderValue.ToString());
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
