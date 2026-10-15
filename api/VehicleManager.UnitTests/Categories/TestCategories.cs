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

    // Theory rows can only carry simple values, so they name a configuration instead of passing one.
    public static List<Category> Configuration(string name) => name switch
    {
        "default" => Default(),
        "empty" => [],
        "single" => [Band("Everything", 0m, null, id: 7)],
        "four" =>
        [
            Band("Light", 0m, 500m, id: 1),
            Band("Medium", 500m, 2500m, id: 2),
            Band("Heavy", 2500m, 5000m, id: 3),
            Band("Super heavy", 5000m, null, id: 4)
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown test configuration.")
    };

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

    public static decimal? KgOrNull(string? value) => value is null ? null : Kg(value);
}
