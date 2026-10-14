namespace VehicleManager.Domain.Categories;

public static class CategoryRules
{
    public const int MaxNameLength = 50;

    // Column size for the icon key; every key in CategoryIcons.Allowed must fit.
    public const int MaxIconLength = 16;

    public const int WeightDecimalPlaces = 2;

    // Ceiling imposed by the decimal(8,2) weight columns.
    public const decimal MaxWeightKg = 999999.99m;

    // 500.000 carries no more precision than 500.00 and is allowed; 500.001 is not.
    internal static bool HasTooManyDecimalPlaces(decimal weightKg) =>
        weightKg != Math.Round(weightKg, WeightDecimalPlaces);
}
