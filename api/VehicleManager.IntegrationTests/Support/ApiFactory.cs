using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleManager.Domain.Entities;
using VehicleManager.Infrastructure.Persistence;

namespace VehicleManager.IntegrationTests.Support;

// Runs the real API against the SQL Server from docker-compose, in a throwaway database that is migrated on start
// and dropped afterwards. SQLite is avoided because EF Core cannot order by decimal columns there.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databaseName = $"VehicleManager_Test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                var configured = provider.GetRequiredService<IConfiguration>().GetConnectionString("Default");
                var connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = databaseName };
                options.UseSqlServer(connection.ConnectionString);
            });
        });
    }

    // Puts the database back to the three shipped categories with no vehicles.
    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Vehicles.ExecuteDeleteAsync();
        await db.Categories.ExecuteDeleteAsync();

        db.Categories.AddRange(
            new Category { Name = "Light", Icon = "car", MinWeightKg = 0m, MaxWeightKg = 500m },
            new Category { Name = "Medium", Icon = "van", MinWeightKg = 500m, MaxWeightKg = 2500m },
            new Category { Name = "Heavy", Icon = "delivery-truck", MinWeightKg = 2500m, MaxWeightKg = null });
        await db.SaveChangesAsync();
    }

    // Touching Server starts the host, which migrates the new database.
    Task IAsyncLifetime.InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
    }
}
