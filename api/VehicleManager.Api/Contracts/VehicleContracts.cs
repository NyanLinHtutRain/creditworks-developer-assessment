namespace VehicleManager.Api.Contracts;

public sealed record ManufacturerResponse(int Id, string Name);

// Fields are nullable so a missing value is reported as a validation error instead of silently becoming zero.
public sealed record VehicleRequest(string? OwnerName, int? ManufacturerId, int? YearOfManufacture, decimal? WeightKg);

public sealed record VehicleCategoryResponse(int Id, string Name, string Icon);

public sealed record VehicleResponse(
    int Id,
    string OwnerName,
    int ManufacturerId,
    string ManufacturerName,
    int YearOfManufacture,
    decimal WeightKg,
    VehicleCategoryResponse? Category);
