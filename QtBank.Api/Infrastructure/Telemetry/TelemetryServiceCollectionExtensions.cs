using System;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace QtBank.Api.Infrastructure.Telemetry;

/// <summary>
/// Service collection extension methods to configure observability and tracing utilizing OpenTelemetry.
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing services with AspNetCore and HttpClient instrumentation, 
    /// as well as Console and OTLP exporters.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The name of the service to register in tracing metadata.</param>
    /// <param name="serviceVersion">The version of the service to register in tracing metadata.</param>
    /// <returns>The service collection with services registered.</returns>
    public static IServiceCollection AddTelemetryServices(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    // Capture spans for incoming HTTP requests (Inbound)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out swagger and favicon requests to keep traces clean
                        options.Filter = context => 
                            !context.Request.Path.StartsWithSegments("/swagger") &&
                            !context.Request.Path.StartsWithSegments("/favicon.ico");
                    })
                    // Capture spans for outgoing HTTP requests made using HttpClient (Outbound)
                    .AddHttpClientInstrumentation()
                    // Export traces to Console (highly useful for local debugging/development)
                    .AddConsoleExporter()
                    // Export traces using standard OTLP (supported by Jaeger, Zipkin, Dynatrace, etc.)
                    .AddOtlpExporter(options =>
                    {
                        // OTLP collector endpoint defaults to http://localhost:4317 (gRPC) or http://localhost:4318 (HTTP).
                        // Can be customized via environment variables such as OTEL_EXPORTER_OTLP_ENDPOINT
                    });
            });

        return services;
    }
}
