using VehicleManager.Domain.Entities;

namespace VehicleManager.Api.Services;

public sealed record CategoryChangeResult(
    ChangeStatus Status, Category? Category = null, IReadOnlyList<string>? Errors = null);
