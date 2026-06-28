using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QtBank.Api.Infrastructure.Middlewares;

/// <summary>
/// Middleware that intercepts HTTP requests to capture or generate a Correlation ID.
/// It enriches the logger scope with the Correlation ID and ensures it is included
/// in the HTTP response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly CorrelationIdOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the HTTP request pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configured Correlation ID options.</param>
    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger,
        IOptions<CorrelationIdOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <returns>A task representing the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Capture or Generate Correlation ID
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var correlationIdValue) ||
            string.IsNullOrWhiteSpace(correlationIdValue))
        {
            correlationIdValue = Guid.NewGuid().ToString();
        }

        var correlationId = correlationIdValue.ToString();

        // Store in HttpContext.Items so that it's accessible within the current request pipeline
        // (e.g. by the DelegatingHandler or other controllers/services)
        context.Items[_options.LogScopeKey] = correlationId;

        // 3. Inclusion in the Response: Inject X-Correlation-Id into the response headers.
        // We do this before calling the next middleware so that the header is present throughout the execution.
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[_options.HeaderName] = correlationId;
        }

        // 2. Log Enrichment: Add the Correlation ID to the log scope (using standard ILogger.BeginScope).
        var logScope = new Dictionary<string, object>
        {
            [_options.LogScopeKey] = correlationId
        };

        using (_logger.BeginScope(logScope))
        {
            _logger.LogInformation("HTTP Request started: {Method} {Path} with CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await _next(context);

            _logger.LogInformation("HTTP Request finished: {Method} {Path} with Status Code: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }
}

/// <summary>
/// Extension methods for registering the Correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Registers the <see cref="CorrelationIdMiddleware"/> in the ASP.NET Core request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder with the middleware registered.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
