using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Supply required test configuration (JWT secret, etc.)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = "ThisIsASecretKeyForTestingPurposesOnly123456!",
                ["Jwt:Secret"]   = "ThisIsASecretKeyForTestingPurposesOnly123456!",
                ["Jwt:Issuer"]   = "TmsTestIssuer",
                ["Jwt:Audience"] = "TmsTestAudience"
            });
        });

        // 2. Remove production DbContext and register InMemory with isolated
        //    internal provider
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TmsDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<TmsDbContext>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<TmsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TmsTestDb");
                options.UseInternalServiceProvider(inMemoryProvider);
            });

            // EnsureCreated creates the schema in-memory.
            // Must run AFTER the DbContext registration so the correct provider is used.
            // context.Database.Migrate() is relational-only and would throw with InMemory.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
