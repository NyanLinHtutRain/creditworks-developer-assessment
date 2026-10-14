namespace VehicleManager.Domain.Entities;

// No category reference: it is derived from WeightKg on read, so category edits apply immediately.
public class Vehicle
{
    public int Id { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public int YearOfManufacture { get; set; }

    public decimal WeightKg { get; set; }

    public int ManufacturerId { get; set; }

    public Manufacturer? Manufacturer { get; set; }
}
