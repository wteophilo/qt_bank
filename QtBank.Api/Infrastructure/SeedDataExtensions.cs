using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Infrastructure;

/// <summary>
/// Contains extension methods for seeding initial/mock data into repositories.
/// </summary>
public static class SeedDataExtensions
{
    /// <summary>
    /// Seeds mock account data for testing purposes in the Swagger UI and integration tests.
    /// </summary>
    public static IApplicationBuilder SeedMockData(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

            var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            accountRepo.SaveAsync(new Account
            {
                Id = aliceId,
                AccountNumber = "111111",
                Balance = 5000.00m,
                OwnerName = "Alice Smith",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                Status = AccountStatus.Active
            }).GetAwaiter().GetResult();

            var bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            accountRepo.SaveAsync(new Account
            {
                Id = bobId,
                AccountNumber = "222222",
                Balance = 150.50m,
                OwnerName = "Bob Johnson",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Status = AccountStatus.Active
            }).GetAwaiter().GetResult();

            var charlieId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            accountRepo.SaveAsync(new Account
            {
                Id = charlieId,
                AccountNumber = "333333",
                Balance = 0.00m,
                OwnerName = "Charlie Davis",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Status = AccountStatus.Inactive
            }).GetAwaiter().GetResult();
        }

        return app;
    }
}
