using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// Changes are boundary movements, since contiguous coverage leaves no hole for a free-standing range to fill.
// Pure: inputs are never modified, and returned categories are reconciled by Id, where 0 means not yet saved.
public static class CategoryConfigurationChanges
{
    // Splits the band holding splitPointKg; the new category takes everything up to that band's former upper bound.
    public static CategoryConfigurationResult Create(
        IEnumerable<Category> existing,
        string? name,
        string? icon,
        decimal splitPointKg)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var ordered = Order(existing);

        if (ordered.Count == 0)
        {
            return CategoryConfigurationValidator.Validate(new[] { Build(name, icon, 0m, null) });
        }

        var bandToSplit = ordered.FirstOrDefault(category => category.Includes(splitPointKg));

        if (bandToSplit is null)
        {
            return CategoryConfigurationResult.Failure(
                $"No category covers {CategoryText.Weight(splitPointKg)} kg, so there is no band to split there.");
        }

        if (bandToSplit.MinWeightKg == splitPointKg)
        {
            return CategoryConfigurationResult.Failure(
                $"Category '{CategoryText.DisplayName(bandToSplit)}' already starts at {CategoryText.Weight(splitPointKg)} kg. " +
                "Choose a weight inside a band rather than one of its edges.");
        }

        var proposed = ordered.Select(Copy).ToList();
        var splitIndex = ordered.IndexOf(bandToSplit);
        var formerUpperBound = bandToSplit.MaxWeightKg;

        proposed[splitIndex].MaxWeightKg = splitPointKg;
        proposed.Insert(splitIndex + 1, Build(name, icon, splitPointKg, formerUpperBound));

        return CategoryConfigurationValidator.Validate(proposed);
    }

    // Moving the lower bound carries the previous category's upper bound with it, since they are the same boundary.
    public static CategoryConfigurationResult Edit(
        IEnumerable<Category> existing,
        int categoryId,
        string? name,
        string? icon,
        decimal minWeightKg)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var ordered = Order(existing);
        var index = ordered.FindIndex(category => category.Id == categoryId);

        if (index < 0)
        {
            return CategoryConfigurationResult.Failure("The category being edited no longer exists.");
        }

        var errors = ValidateBoundMove(ordered, index, minWeightKg);

        if (errors.Count > 0)
        {
            return CategoryConfigurationResult.Failure(errors);
        }

        var proposed = ordered.Select(Copy).ToList();

        proposed[index].Name = name?.Trim() ?? string.Empty;
        proposed[index].Icon = icon?.Trim() ?? string.Empty;
        proposed[index].MinWeightKg = minWeightKg;

        if (index > 0)
        {
            proposed[index - 1].MaxWeightKg = minWeightKg;
        }

        return CategoryConfigurationValidator.Validate(proposed);
    }

    // The band below absorbs the freed range, or the band above drops to zero when the lowest is deleted.
    public static CategoryConfigurationResult Delete(IEnumerable<Category> existing, int categoryId)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var ordered = Order(existing);
        var index = ordered.FindIndex(category => category.Id == categoryId);

        if (index < 0)
        {
            return CategoryConfigurationResult.Failure("The category being deleted no longer exists.");
        }

        if (ordered.Count == 1)
        {
            return CategoryConfigurationResult.Failure(
                $"'{CategoryText.DisplayName(ordered[0])}' is the only category and cannot be deleted. " +
                "At least one category must remain so that every vehicle weight has one.");
        }

        var proposed = ordered.Select(Copy).ToList();
        var removed = proposed[index];
        proposed.RemoveAt(index);

        if (index == 0)
        {
            proposed[0].MinWeightKg = 0m;
        }
        else
        {
            proposed[index - 1].MaxWeightKg = removed.MaxWeightKg;
        }

        return CategoryConfigurationValidator.Validate(proposed);
    }

    // Checked before rebuilding so the caller hears about the move it asked for, not a generic coverage complaint.
    private static List<string> ValidateBoundMove(List<Category> ordered, int index, decimal minWeightKg)
    {
        var errors = new List<string>();
        var category = ordered[index];

        if (CategoryRules.HasTooManyDecimalPlaces(minWeightKg))
        {
            errors.Add(
                $"The lower weight bound for category '{CategoryText.DisplayName(category)}' " +
                $"must have at most {CategoryRules.WeightDecimalPlaces} decimal places.");
            return errors;
        }

        if (index == 0)
        {
            if (minWeightKg != 0m)
            {
                errors.Add(
                    $"'{CategoryText.DisplayName(category)}' is the lowest category and must start at 0.00 kg. " +
                    "To change where it ends, edit the lower bound of the category above it.");
            }

            return errors;
        }

        var below = ordered[index - 1];

        if (minWeightKg <= below.MinWeightKg)
        {
            errors.Add(
                $"{CategoryText.Weight(minWeightKg)} kg is not above '{CategoryText.DisplayName(below)}', which starts at " +
                $"{CategoryText.Weight(below.MinWeightKg)} kg. '{CategoryText.DisplayName(category)}' must start above the category below it.");
        }

        if (index < ordered.Count - 1)
        {
            var above = ordered[index + 1];

            if (minWeightKg >= above.MinWeightKg)
            {
                errors.Add(
                    $"{CategoryText.Weight(minWeightKg)} kg is not below '{CategoryText.DisplayName(above)}', which starts at " +
                    $"{CategoryText.Weight(above.MinWeightKg)} kg. '{CategoryText.DisplayName(category)}' must start below the category above it.");
            }
        }

        return errors;
    }

    private static List<Category> Order(IEnumerable<Category> categories) =>
        categories.OrderBy(category => category.MinWeightKg).ToList();

    private static Category Build(string? name, string? icon, decimal minWeightKg, decimal? maxWeightKg) => new()
    {
        Name = name?.Trim() ?? string.Empty,
        Icon = icon?.Trim() ?? string.Empty,
        MinWeightKg = minWeightKg,
        MaxWeightKg = maxWeightKg
    };

    private static Category Copy(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Icon = category.Icon,
        MinWeightKg = category.MinWeightKg,
        MaxWeightKg = category.MaxWeightKg
    };
}
