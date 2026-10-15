namespace VehicleManager.Api.Services;

public sealed record VehicleCreateResult(VehicleWithCategory? Created, IReadOnlyList<string> Errors);
