using System.Globalization;
using VehicleManager.Domain.Entities;

namespace VehicleManager.UnitTests.Categories;

internal static class TestCategories
{
    public const int LightId = 1;
    public const int MediumId = 2;
    public const int HeavyId = 3;

    // The configuration the application ships with.
    public static List<Category> Default() =>
    [
        new Category { Id = LightId, Name = "Light", Icon = "car", MinWeightKg = 0m, MaxWeightKg = 500m },
        new Category { Id = MediumId, Name = "Medium", Icon = "van", MinWeightKg = 500m, MaxWeightKg = 2500m },
        new Category { Id = HeavyId, Name = "Heavy", Icon = "delivery-truck", MinWeightKg = 2500m, MaxWeightKg = null }
    ];

    public static Category Band(string name, decimal minWeightKg, decimal? maxWeightKg, int id = 0) => new()
    {
        Id = id,
        Name = name,
        Icon = "car",
        MinWeightKg = minWeightKg,
        MaxWeightKg = maxWeightKg
    };

    // Weights are given as text so test values are never rounded through a floating-point type.
    public static decimal Kg(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
