using VehicleManager.Domain.Categories;

namespace VehicleManager.Domain.Vehicles;

public static class VehicleValidator
{
    // Inputs are nullable so a missing value is reported as "required" instead of being read as zero.
    // Whether the manufacturer exists is checked against the database by the caller.
    public static IReadOnlyList<string> Validate(
        string? ownerName, int? manufacturerId, int? yearOfManufacture, decimal? weightKg, int currentYear)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            errors.Add("Owner's name is required.");
        }
        else if (ownerName.Trim().Length > VehicleRules.MaxOwnerNameLength)
        {
            errors.Add($"Owner's name must be {VehicleRules.MaxOwnerNameLength} characters or fewer.");
        }

        if (manufacturerId is null)
        {
            errors.Add("A manufacturer is required.");
        }

        ValidateYear(yearOfManufacture, currentYear, errors);
        ValidateWeight(weightKg, errors);

        return errors;
    }

    // A model can be built a year ahead of its release, so next year is still sensible.
    private static void ValidateYear(int? year, int currentYear, List<string> errors)
    {
        if (year is null)
        {
            errors.Add("Year of manufacture is required.");
        }
        else if (year < VehicleRules.MinYear || year > currentYear + 1)
        {
            errors.Add($"Year of manufacture must be between {VehicleRules.MinYear} and {currentYear + 1}.");
        }
    }

    // Vehicle weights share the category weight limits, which come from the decimal(8,2) columns.
    private static void ValidateWeight(decimal? weightKg, List<string> errors)
    {
        if (weightKg is not { } weight)
        {
            errors.Add("Weight is required.");
        }
        else if (weight <= 0)
        {
            errors.Add("Weight must be greater than zero.");
        }
        else if (weight > CategoryRules.MaxWeightKg)
        {
            errors.Add($"Weight cannot exceed {CategoryText.Weight(CategoryRules.MaxWeightKg)} kg.");
        }
        else if (CategoryRules.HasTooManyDecimalPlaces(weight))
        {
            errors.Add($"Weight must have at most {CategoryRules.WeightDecimalPlaces} decimal places.");
        }
    }
}
