using System;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace QtBank.Api.Infrastructure.Telemetry;

/// <summary>
/// Service collection extension methods to configure observability, tracing, and metrics utilizing OpenTelemetry.
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing and metrics services with AspNetCore, HttpClient, and Runtime instrumentation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The name of the service to register in telemetry metadata.</param>
    /// <param name="serviceVersion">The version of the service to register in telemetry metadata.</param>
    /// <returns>The service collection with services registered.</returns>
    public static IServiceCollection AddTelemetryServices(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register custom application metrics helper
        services.AddSingleton<ApplicationMetrics>();

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
            })
            .WithMetrics(metrics =>
            {
                metrics
                    // Capture default inbound ASP.NET Core HTTP metrics (requests count, duration, status codes)
                    .AddAspNetCoreInstrumentation()
                    // Capture outbound HttpClient metrics
                    .AddHttpClientInstrumentation()
                    // Capture .NET runtime metrics (CPU, Memory, GC, ThreadPool)
                    .AddRuntimeInstrumentation()
                    // Register custom application metrics source
                    .AddMeter(ApplicationMetrics.MeterName)
                    // Export metrics to Console
                    .AddConsoleExporter();
            });

        return services;
    }
}
