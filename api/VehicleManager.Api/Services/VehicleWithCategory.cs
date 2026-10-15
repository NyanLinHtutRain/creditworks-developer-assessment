using VehicleManager.Domain.Entities;

namespace VehicleManager.Api.Services;

// The category is resolved from the weight at read time and is never stored on the vehicle.
public sealed record VehicleWithCategory(Vehicle Vehicle, Category? Category);
