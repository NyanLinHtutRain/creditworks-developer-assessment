using VehicleManager.Api.Contracts;
using VehicleManager.Api.ErrorHandling;
using VehicleManager.Api.Services;

namespace VehicleManager.Api.Endpoints;

public static class VehicleEndpoints
{
    // Only these names are accepted, so a caller can never choose an arbitrary column to sort by.
    private static readonly Dictionary<string, VehicleSortField> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["owner"] = VehicleSortField.Owner,
        ["manufacturer"] = VehicleSortField.Manufacturer,
        ["year"] = VehicleSortField.Year,
        ["weight"] = VehicleSortField.Weight
    };

    public static void MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vehicles").WithTags("Vehicles");

        group.MapGet("/", async (
            string? sortBy, string? direction, VehicleService service, CancellationToken cancellationToken) =>
        {
            var errors = new List<string>();
            var field = VehicleSortField.Owner;
            var descending = false;

            if (sortBy is not null && !SortFields.TryGetValue(sortBy, out field))
            {
                errors.Add("sortBy must be one of: owner, manufacturer, year, weight.");
            }

            if (direction is not null)
            {
                descending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

                if (!descending && !direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("direction must be asc or desc.");
                }
            }

            if (errors.Count > 0)
            {
                return ProblemResults.Invalid("The sort options are not valid.", errors);
            }

            var vehicles = await service.ListAsync(field, descending, cancellationToken);

            return Results.Ok(vehicles.Select(ToResponse));
        });

        group.MapPost("/", async (VehicleRequest request, VehicleService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(
                request.OwnerName, request.ManufacturerId, request.YearOfManufacture, request.WeightKg, cancellationToken);

            return result.Created is { } created
                ? Results.Json(ToResponse(created), statusCode: StatusCodes.Status201Created)
                : ProblemResults.Invalid("The vehicle is not valid.", result.Errors);
        });
    }

    // The manufacturer is loaded on every path that reaches here.
    private static VehicleResponse ToResponse(VehicleWithCategory item) => new(
        item.Vehicle.Id,
        item.Vehicle.OwnerName,
        item.Vehicle.ManufacturerId,
        item.Vehicle.Manufacturer!.Name,
        item.Vehicle.YearOfManufacture,
        item.Vehicle.WeightKg,
        item.Category is { } category ? new VehicleCategoryResponse(category.Id, category.Name, category.Icon) : null);
}
