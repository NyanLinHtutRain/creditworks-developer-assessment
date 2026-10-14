using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// Changes are boundary movements, since contiguous coverage leaves no hole for a free-standing range to fill.
// Pure: inputs are never modified, and returned categories are reconciled by Id, where 0 means not yet saved.
public static class CategoryConfigurationChanges
{
    // The new range must start or end where an existing category does; that category shrinks to make room.
    public static CategoryConfigurationResult Create(
        IEnumerable<Category> existing,
        string? name,
        string? icon,
        decimal minWeightKg,
        decimal? maxWeightKg)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var rangeError = CheckNewRange(minWeightKg, maxWeightKg);

        if (rangeError is not null)
        {
            return CategoryConfigurationResult.Failure(rangeError);
        }

        var ordered = Order(existing);
        var created = Build(name, icon, minWeightKg, maxWeightKg);

        if (ordered.Count == 0)
        {
            return CategoryConfigurationValidator.Validate(new[] { created });
        }

        var band = ordered.FirstOrDefault(category => category.Includes(minWeightKg));

        if (band is null)
        {
            return CategoryConfigurationResult.Failure(
                $"No category covers {CategoryText.Weight(minWeightKg)} kg, so a new category cannot start there.");
        }

        var bandName = CategoryText.DisplayName(band);

        if (band.MaxWeightKg is { } bandEnd && (maxWeightKg is null || maxWeightKg > bandEnd))
        {
            return CategoryConfigurationResult.Failure(
                $"The new range runs past the end of '{bandName}' ({CategoryText.Range(band)}). " +
                "A new category must fit inside one existing category.");
        }

        var sharesStart = minWeightKg == band.MinWeightKg;
        var sharesEnd = maxWeightKg == band.MaxWeightKg;

        if (sharesStart && sharesEnd)
        {
            return CategoryConfigurationResult.Failure($"'{bandName}' already covers exactly that range.");
        }

        if (!sharesStart && !sharesEnd)
        {
            return CategoryConfigurationResult.Failure(
                "A new category must start or end where an existing one does. " +
                $"This range sits inside '{bandName}' ({CategoryText.Range(band)}) without touching either end.");
        }

        var proposed = ordered.Select(Copy).ToList();
        var index = ordered.IndexOf(band);

        // Sharing the start with a closed new range means the new category takes the lower piece.
        if (sharesStart && maxWeightKg is { } pieceEnd)
        {
            proposed[index].MinWeightKg = pieceEnd;
            proposed.Insert(index, created);
        }
        else
        {
            proposed[index].MaxWeightKg = minWeightKg;
            proposed.Insert(index + 1, created);
        }

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

    private static string? CheckNewRange(decimal minWeightKg, decimal? maxWeightKg)
    {
        if (CategoryRules.HasTooManyDecimalPlaces(minWeightKg) ||
            (maxWeightKg is { } preciseEnd && CategoryRules.HasTooManyDecimalPlaces(preciseEnd)))
        {
            return $"Weights must have at most {CategoryRules.WeightDecimalPlaces} decimal places.";
        }

        if (maxWeightKg is { } end && end <= minWeightKg)
        {
            return $"The upper weight bound must be greater than the lower bound of {CategoryText.Weight(minWeightKg)} kg.";
        }

        return null;
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
