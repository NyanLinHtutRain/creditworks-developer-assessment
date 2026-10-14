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

        var rangeError = CheckRange(minWeightKg, maxWeightKg);

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

    // Each bound is shared with a neighbour, so moving one carries the neighbour's matching bound with it.
    public static CategoryConfigurationResult Edit(
        IEnumerable<Category> existing,
        int categoryId,
        string? name,
        string? icon,
        decimal minWeightKg,
        decimal? maxWeightKg)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var ordered = Order(existing);
        var index = ordered.FindIndex(category => category.Id == categoryId);

        if (index < 0)
        {
            return CategoryConfigurationResult.Failure("The category being edited no longer exists.");
        }

        var errors = ValidateRangeMove(ordered, index, minWeightKg, maxWeightKg);

        if (errors.Count > 0)
        {
            return CategoryConfigurationResult.Failure(errors);
        }

        var proposed = ordered.Select(Copy).ToList();

        proposed[index].Name = name?.Trim() ?? string.Empty;
        proposed[index].Icon = icon?.Trim() ?? string.Empty;
        proposed[index].MinWeightKg = minWeightKg;
        proposed[index].MaxWeightKg = maxWeightKg;

        if (index > 0)
        {
            proposed[index - 1].MaxWeightKg = minWeightKg;
        }

        if (index < proposed.Count - 1 && maxWeightKg is { } end)
        {
            proposed[index + 1].MinWeightKg = end;
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
    private static List<string> ValidateRangeMove(
        List<Category> ordered, int index, decimal minWeightKg, decimal? maxWeightKg)
    {
        var rangeError = CheckRange(minWeightKg, maxWeightKg);

        if (rangeError is not null)
        {
            return [rangeError];
        }

        var errors = new List<string>();
        var name = CategoryText.DisplayName(ordered[index]);
        var isLowest = index == 0;
        var isHighest = index == ordered.Count - 1;

        if (isLowest && minWeightKg != 0m)
        {
            errors.Add($"'{name}' is the lowest category and must start at 0.00 kg.");
        }

        if (isHighest && maxWeightKg is not null)
        {
            errors.Add(
                $"'{name}' is the highest category and must stay open-ended, so it cannot have an upper bound. " +
                "To end it sooner, add a category above it.");
        }

        if (!isHighest && maxWeightKg is null)
        {
            errors.Add($"'{name}' is not the highest category, so it needs an upper bound.");
        }

        if (!isLowest && minWeightKg <= ordered[index - 1].MinWeightKg)
        {
            var below = ordered[index - 1];

            errors.Add(
                $"{CategoryText.Weight(minWeightKg)} kg is not above '{CategoryText.DisplayName(below)}', which starts at " +
                $"{CategoryText.Weight(below.MinWeightKg)} kg. '{name}' must start above the category below it.");
        }

        if (!isHighest && maxWeightKg is { } end && ordered[index + 1].MaxWeightKg is { } aboveEnd && end >= aboveEnd)
        {
            errors.Add(
                $"{CategoryText.Weight(end)} kg is not below the end of '{CategoryText.DisplayName(ordered[index + 1])}' " +
                $"({CategoryText.Weight(aboveEnd)} kg), so it would leave no room for it.");
        }

        return errors;
    }

    private static string? CheckRange(decimal minWeightKg, decimal? maxWeightKg)
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
