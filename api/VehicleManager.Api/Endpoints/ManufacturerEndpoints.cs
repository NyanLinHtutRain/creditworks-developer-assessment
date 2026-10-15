using Microsoft.EntityFrameworkCore;
using VehicleManager.Api.Contracts;
using VehicleManager.Infrastructure.Persistence;

namespace VehicleManager.Api.Endpoints;

public static class ManufacturerEndpoints
{
    // A read-only lookup, so it queries directly instead of going through a service.
    public static void MapManufacturerEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/manufacturers", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.Manufacturers
                .AsNoTracking()
                .OrderBy(manufacturer => manufacturer.Name)
                .Select(manufacturer => new ManufacturerResponse(manufacturer.Id, manufacturer.Name))
                .ToListAsync(cancellationToken)))
            .WithTags("Manufacturers");
}
