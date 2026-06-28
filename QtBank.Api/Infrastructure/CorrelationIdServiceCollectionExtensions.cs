using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QtBank.Api.Infrastructure.Http;
using QtBank.Api.Infrastructure.Middlewares;

namespace QtBank.Api.Infrastructure;

/// <summary>
/// Service collection extension methods to register Correlation ID dependencies.
/// </summary>
public static class CorrelationIdServiceCollectionExtensions
{
    /// <summary>
    /// Adds Correlation ID services, including HttpContextAccessor and the custom DelegatingHandler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional delegate to configure Correlation ID options.</param>
    /// <returns>The service collection with services registered.</returns>
    public static IServiceCollection AddCorrelationIdServices(
        this IServiceCollection services,
        Action<CorrelationIdOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 1. Register HttpContextAccessor if not already registered
        services.AddHttpContextAccessor();

        // 2. Configure Options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<CorrelationIdOptions>(_ => { });
        }

        // 3. Register the Delegating Handler as transient so it can be added to HttpClients
        services.TryAddTransient<CorrelationIdDelegatingHandler>();

        return services;
    }
}
