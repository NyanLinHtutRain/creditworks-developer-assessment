using Microsoft.EntityFrameworkCore;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using VehicleManager.Domain.Vehicles;

namespace VehicleManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.Property(manufacturer => manufacturer.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(manufacturer => manufacturer.Name).IsUnique();
        });

        // The database cannot check that ranges are gap-free, since that spans rows; the category service does.
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(CategoryRules.MaxNameLength).IsRequired();
            entity.Property(category => category.Icon).HasMaxLength(CategoryRules.MaxIconLength).IsRequired();
            entity.Property(category => category.MinWeightKg).HasPrecision(8, 2);
            entity.Property(category => category.MaxWeightKg).HasPrecision(8, 2);

            // SQL Server's default collation is case-insensitive, matching the validator's name check.
            entity.HasIndex(category => category.Name).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Categories_MinWeightKg", "[MinWeightKg] >= 0");
                table.HasCheckConstraint("CK_Categories_MaxWeightKg", "[MaxWeightKg] IS NULL OR [MaxWeightKg] > [MinWeightKg]");
            });
        });

        // No category column: a vehicle's category is derived from its weight on read.
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(vehicle => vehicle.OwnerName).HasMaxLength(VehicleRules.MaxOwnerNameLength).IsRequired();
            entity.Property(vehicle => vehicle.WeightKg).HasPrecision(8, 2);

            entity.HasOne(vehicle => vehicle.Manufacturer)
                .WithMany()
                .HasForeignKey(vehicle => vehicle.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table => table.HasCheckConstraint("CK_Vehicles_WeightKg", "[WeightKg] > 0"));
        });
    }
}
