using System.Net.Http.Json;
using FluentAssertions;
using VehicleManager.Api.Contracts;
using VehicleManager.IntegrationTests.Support;

namespace VehicleManager.IntegrationTests;

// Reads the categories without the reset the other tests do, so a wrong value in the migration's seed data fails here.
public class SeedDataTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_new_database_holds_the_three_shipped_categories()
    {
        var categories = await factory.CreateClient().GetFromJsonAsync<List<CategoryResponse>>("/api/categories");

        categories.Should().BeEquivalentTo(
            new[]
            {
                new { Name = "Light", Icon = "car", MinWeightKg = 0m, MaxWeightKg = (decimal?)500m },
                new { Name = "Medium", Icon = "van", MinWeightKg = 500m, MaxWeightKg = (decimal?)2500m },
                new { Name = "Heavy", Icon = "delivery-truck", MinWeightKg = 2500m, MaxWeightKg = (decimal?)null }
            },
            options => options.WithStrictOrdering());
    }
}
