using System.Globalization;
using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// Validates a complete set: a single category cannot be judged alone, since correctness depends on its neighbours.
public static class CategoryConfigurationValidator
{
    public static CategoryConfigurationResult Validate(IEnumerable<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var ordered = categories.OrderBy(category => category.MinWeightKg).ToList();

        if (ordered.Count == 0)
        {
            return CategoryConfigurationResult.Failure(
                "At least one category must be defined so that every vehicle weight has a category.");
        }

        var errors = new List<string>();
        var boundsAreSound = true;

        for (var index = 0; index < ordered.Count; index++)
        {
            var label = Describe(ordered[index], index);

            ValidateText(ordered[index], label, errors);
            boundsAreSound &= ValidateBounds(ordered[index], label, errors);
        }

        ValidateNamesAreUnique(ordered, errors);

        // One unusable bound would otherwise produce a cascade of misleading gap and overlap messages.
        if (boundsAreSound)
        {
            ValidateCoverage(ordered, errors);
        }

        return errors.Count == 0
            ? CategoryConfigurationResult.Success(ordered)
            : CategoryConfigurationResult.Failure(errors);
    }

    private static void ValidateText(Category category, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            errors.Add($"A name is required for {label}.");
        }
        else if (category.Name.Trim().Length > CategoryRules.MaxNameLength)
        {
            errors.Add($"The name for {label} must be {CategoryRules.MaxNameLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(category.Icon))
        {
            errors.Add($"An icon is required for {label}.");
        }
        else if (category.Icon.Trim().Length > CategoryRules.MaxIconLength)
        {
            errors.Add($"The icon for {label} must be {CategoryRules.MaxIconLength} characters or fewer.");
        }
    }

    // Returns false when a bound is unusable for the coverage checks that follow.
    private static bool ValidateBounds(Category category, string label, List<string> errors)
    {
        var sound = true;

        if (category.MinWeightKg < 0)
        {
            errors.Add($"The lower weight bound for {label} cannot be negative.");
            sound = false;
        }
        else if (category.MinWeightKg > CategoryRules.MaxWeightKg)
        {
            errors.Add($"The lower weight bound for {label} cannot exceed {Format(CategoryRules.MaxWeightKg)} kg.");
            sound = false;
        }

        if (HasTooManyDecimalPlaces(category.MinWeightKg))
        {
            errors.Add($"The lower weight bound for {label} must have at most {CategoryRules.WeightDecimalPlaces} decimal places.");
            sound = false;
        }

        if (category.MaxWeightKg is not { } max)
        {
            return sound;
        }

        if (max > CategoryRules.MaxWeightKg)
        {
            errors.Add($"The upper weight bound for {label} cannot exceed {Format(CategoryRules.MaxWeightKg)} kg.");
            sound = false;
        }

        if (HasTooManyDecimalPlaces(max))
        {
            errors.Add($"The upper weight bound for {label} must have at most {CategoryRules.WeightDecimalPlaces} decimal places.");
            sound = false;
        }

        if (max <= category.MinWeightKg)
        {
            errors.Add(
                $"The upper weight bound for {label} must be greater than its lower bound of {Format(category.MinWeightKg)} kg.");
            sound = false;
        }

        return sound;
    }

    private static void ValidateNamesAreUnique(List<Category> ordered, List<string> errors)
    {
        var duplicates = ordered
            .Where(category => !string.IsNullOrWhiteSpace(category.Name))
            .GroupBy(category => category.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            errors.Add($"The category name '{duplicate.Key}' is used more than once. Category names must be unique.");
        }
    }

    // Coverage holds when the lowest band starts at zero, each upper bound meets the next lower bound, and the highest is open-ended.
    private static void ValidateCoverage(List<Category> ordered, List<string> errors)
    {
        ValidateOpenEndedBand(ordered, errors);

        if (ordered[0].MinWeightKg != 0m)
        {
            errors.Add(
                $"The lowest category '{Name(ordered[0])}' must start at 0.00 kg so that every weight has a category, " +
                $"but it starts at {Format(ordered[0].MinWeightKg)} kg.");
        }

        for (var index = 0; index < ordered.Count - 1; index++)
        {
            var lower = ordered[index];
            var upper = ordered[index + 1];

            // An open-ended band below the top is already reported by ValidateOpenEndedBand.
            if (lower.MaxWeightKg is not { } boundary)
            {
                continue;
            }

            if (boundary < upper.MinWeightKg)
            {
                errors.Add(
                    $"A gap exists between {Format(boundary)} kg and {Format(upper.MinWeightKg)} kg. " +
                    "Weights in that range would have no category.");
            }
            else if (boundary > upper.MinWeightKg)
            {
                errors.Add(
                    $"Categories '{Name(lower)}' and '{Name(upper)}' overlap between " +
                    $"{Format(upper.MinWeightKg)} kg and {Format(boundary)} kg.");
            }
        }
    }

    private static void ValidateOpenEndedBand(List<Category> ordered, List<string> errors)
    {
        var openEnded = ordered.Where(category => category.MaxWeightKg is null).ToList();

        if (openEnded.Count == 0)
        {
            errors.Add(
                "One category must be open-ended, with no upper bound, so that any weight above the highest " +
                "boundary still has a category.");
        }
        else if (openEnded.Count > 1)
        {
            errors.Add(
                $"Only one category can be open-ended, but {openEnded.Count} have no upper bound " +
                $"({string.Join(", ", openEnded.Select(category => $"'{Name(category)}'"))}).");
        }
        else if (!ReferenceEquals(openEnded[0], ordered[^1]))
        {
            errors.Add($"The open-ended category '{Name(openEnded[0])}' must be the highest category.");
        }
    }

    // 500.000 carries no more precision than 500.00 and is allowed; 500.001 is not.
    private static bool HasTooManyDecimalPlaces(decimal value) =>
        value != Math.Round(value, CategoryRules.WeightDecimalPlaces);

    private static string Format(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Name(Category category) =>
        string.IsNullOrWhiteSpace(category.Name) ? "(unnamed)" : category.Name.Trim();

    private static string Describe(Category category, int index) =>
        string.IsNullOrWhiteSpace(category.Name)
            ? $"category {index + 1}"
            : $"category '{category.Name.Trim()}'";
}
