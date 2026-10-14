using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// Resolves in memory rather than in SQL so Category.Includes stays the only copy of the boundary rule.
public static class CategoryResolver
{
    public static Category? Resolve(IEnumerable<Category> categories, decimal weightKg)
    {
        ArgumentNullException.ThrowIfNull(categories);

        foreach (var category in categories)
        {
            if (category.Includes(weightKg))
            {
                return category;
            }
        }

        return null;
    }
}
