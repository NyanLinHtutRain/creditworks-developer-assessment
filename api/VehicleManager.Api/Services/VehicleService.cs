using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using VehicleManager.Domain.Vehicles;
using VehicleManager.Infrastructure.Persistence;

namespace VehicleManager.Api.Services;

public sealed class VehicleService(AppDbContext db, TimeProvider clock, ILogger<VehicleService> logger)
{
    // Sorting happens in SQL. Categories are loaded once and matched in memory, so the boundary rule has one definition.
    public async Task<IReadOnlyList<VehicleWithCategory>> ListAsync(
        VehicleSortField field, bool descending, CancellationToken cancellationToken)
    {
        var vehicles = await Sort(db.Vehicles.AsNoTracking().Include(vehicle => vehicle.Manufacturer), field, descending)
            .ThenBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);

        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);

        return vehicles
            .Select(vehicle => new VehicleWithCategory(vehicle, CategoryResolver.Resolve(categories, vehicle.WeightKg)))
            .ToList();
    }

    public async Task<VehicleCreateResult> CreateAsync(
        string? ownerName, int? manufacturerId, int? yearOfManufacture, decimal? weightKg, CancellationToken cancellationToken)
    {
        var errors = VehicleValidator
            .Validate(ownerName, manufacturerId, yearOfManufacture, weightKg, clock.GetUtcNow().Year)
            .ToList();

        Manufacturer? manufacturer = null;

        if (manufacturerId is { } id)
        {
            manufacturer = await db.Manufacturers.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

            if (manufacturer is null)
            {
                errors.Add("The selected manufacturer does not exist.");
            }
        }

        if (errors.Count > 0)
        {
            logger.LogInformation("Vehicle rejected with {ErrorCount} validation error(s)", errors.Count);
            return new VehicleCreateResult(null, errors);
        }

        // Validation above guarantees every value is present.
        var vehicle = new Vehicle
        {
            OwnerName = ownerName!.Trim(),
            Manufacturer = manufacturer,
            YearOfManufacture = yearOfManufacture!.Value,
            WeightKg = weightKg!.Value
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);

        logger.LogInformation("Vehicle {VehicleId} created", vehicle.Id);

        return new VehicleCreateResult(
            new VehicleWithCategory(vehicle, CategoryResolver.Resolve(categories, vehicle.WeightKg)), []);
    }

    private static IOrderedQueryable<Vehicle> Sort(IQueryable<Vehicle> query, VehicleSortField field, bool descending) =>
        field switch
        {
            VehicleSortField.Owner => Order(query, vehicle => vehicle.OwnerName, descending),
            VehicleSortField.Manufacturer => Order(query, vehicle => vehicle.Manufacturer!.Name, descending),
            VehicleSortField.Year => Order(query, vehicle => vehicle.YearOfManufacture, descending),
            VehicleSortField.Weight => Order(query, vehicle => vehicle.WeightKg, descending),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown sort field.")
        };

    private static IOrderedQueryable<Vehicle> Order<TKey>(
        IQueryable<Vehicle> query, Expression<Func<Vehicle, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
