namespace QtBank.Api.Infrastructure.Middlewares;

/// <summary>
/// Options for configuring the Correlation ID middleware and delegating handler.
/// </summary>
public sealed class CorrelationIdOptions
{
    /// <summary>
    /// The HTTP header name used to transport the Correlation ID.
    /// Defaults to "X-Correlation-Id".
    /// </summary>
    public string HeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// The key used to store the Correlation ID in the logger scope dictionary
    /// and HttpContext.Items. Defaults to "CorrelationId".
    /// </summary>
    public string LogScopeKey { get; set; } = "CorrelationId";
}
