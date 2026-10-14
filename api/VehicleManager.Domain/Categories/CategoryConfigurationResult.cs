using VehicleManager.Domain.Entities;

namespace VehicleManager.Domain.Categories;

// An invalid configuration is an expected result of user input, so it is returned rather than thrown.
public sealed class CategoryConfigurationResult
{
    private CategoryConfigurationResult(IReadOnlyList<Category> categories, IReadOnlyList<string> errors)
    {
        Categories = categories;
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    // Ordered by lower bound, and empty when the configuration was rejected.
    public IReadOnlyList<Category> Categories { get; }

    public static CategoryConfigurationResult Success(IReadOnlyList<Category> categories) =>
        new(categories, Array.Empty<string>());

    public static CategoryConfigurationResult Failure(IReadOnlyList<string> errors) =>
        new(Array.Empty<Category>(), errors);

    public static CategoryConfigurationResult Failure(string error) =>
        Failure(new[] { error });
}
