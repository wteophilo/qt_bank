using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Repositories;

namespace QtBank.Api.Infrastructure.Outbox;

/// <summary>
/// Service collection extension methods to configure Outbox components.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Outbox database repository and the background worker processor.
    /// </summary>
    public static IServiceCollection AddOutboxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration options section
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));

        // 1. Register the in-memory outbox repository as a singleton to share state across scopes
        services.TryAddSingleton<IOutboxRepository, InMemoryOutboxRepository>();

        // 2. Register the Outbox Background processor worker
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
