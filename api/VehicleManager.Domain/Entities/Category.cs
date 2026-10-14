namespace VehicleManager.Domain.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public decimal MinWeightKg { get; set; }

    // Null means open-ended, the "and above" band.
    public decimal? MaxWeightKg { get; set; }

    // The only expression of the boundary rule: lower bound inclusive, upper bound exclusive.
    public bool Includes(decimal weightKg) =>
        weightKg >= MinWeightKg && (MaxWeightKg is null || weightKg < MaxWeightKg.Value);
}
