using VehicleManager.Domain.Entities;

namespace VehicleManager.Api.Contracts;

// Fields are nullable so a missing value is reported as a validation error instead of silently becoming zero.
public sealed record CategoryRequest(string? Name, string? Icon, decimal? MinWeightKg, decimal? MaxWeightKg);

public sealed record CategoryResponse(int Id, string Name, string Icon, decimal MinWeightKg, decimal? MaxWeightKg)
{
    public static CategoryResponse From(Category category) =>
        new(category.Id, category.Name, category.Icon, category.MinWeightKg, category.MaxWeightKg);
}
