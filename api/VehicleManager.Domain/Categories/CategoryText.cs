using System.Globalization;
using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// Shared wording helpers so every validation message shows weights and names the same way.
internal static class CategoryText
{
    public static string Weight(decimal weightKg) =>
        weightKg.ToString("0.00", CultureInfo.InvariantCulture);

    public static string DisplayName(Category category) =>
        string.IsNullOrWhiteSpace(category.Name) ? "(unnamed)" : category.Name.Trim();
}
