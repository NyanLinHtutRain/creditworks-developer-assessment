using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using VehicleManager.Infrastructure.Persistence;

namespace VehicleManager.Api.Services;

// Applies a domain change to the stored categories in one serializable transaction, so two edits cannot interleave.
public sealed class CategoryService(AppDbContext db, ILogger<CategoryService> logger)
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking().OrderBy(category => category.MinWeightKg).ToListAsync(cancellationToken);

    public Task<CategoryChangeResult> CreateAsync(
        string? name, string? icon, decimal minWeightKg, decimal? maxWeightKg, CancellationToken cancellationToken) =>
        ApplyAsync(
            "create", null,
            categories => CategoryConfigurationChanges.Create(categories, name, icon, minWeightKg, maxWeightKg),
            cancellationToken);

    public Task<CategoryChangeResult> EditAsync(
        int id, string? name, string? icon, decimal minWeightKg, decimal? maxWeightKg, CancellationToken cancellationToken) =>
        ApplyAsync(
            "edit", id,
            categories => CategoryConfigurationChanges.Edit(categories, id, name, icon, minWeightKg, maxWeightKg),
            cancellationToken);

    public Task<CategoryChangeResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        ApplyAsync("delete", id, categories => CategoryConfigurationChanges.Delete(categories, id), cancellationToken);

    private async Task<CategoryChangeResult> ApplyAsync(
        string operation,
        int? id,
        Func<List<Category>, CategoryConfigurationResult> change,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var stored = await db.Categories.OrderBy(category => category.MinWeightKg).ToListAsync(cancellationToken);

        if (id is { } categoryId && stored.All(category => category.Id != categoryId))
        {
            return new CategoryChangeResult(ChangeStatus.NotFound);
        }

        var result = change(stored);

        if (!result.IsValid)
        {
            logger.LogInformation(
                "Category {Operation} rejected with {ErrorCount} validation error(s)", operation, result.Errors.Count);
            return new CategoryChangeResult(ChangeStatus.Invalid, Errors: result.Errors);
        }

        var added = Reconcile(stored, result.Categories);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Boundaries only, so the audit line never contains user-supplied text.
        logger.LogInformation(
            "Category {Operation} applied. Configuration is now {Configuration}", operation, Describe(result.Categories));

        var subject = added ?? (id is { } editedId && result.Categories.Any(category => category.Id == editedId)
            ? stored.Single(category => category.Id == editedId)
            : null);

        return new CategoryChangeResult(ChangeStatus.Success, subject);
    }

    // Copies the proposed set onto the tracked rows, so EF issues only the inserts, updates and deletes needed.
    private Category? Reconcile(List<Category> stored, IReadOnlyList<Category> proposed)
    {
        Category? added = null;

        foreach (var next in proposed)
        {
            if (next.Id == 0)
            {
                db.Categories.Add(next);
                added = next;
                continue;
            }

            var current = stored.Single(category => category.Id == next.Id);
            current.Name = next.Name;
            current.Icon = next.Icon;
            current.MinWeightKg = next.MinWeightKg;
            current.MaxWeightKg = next.MaxWeightKg;
        }

        foreach (var removed in stored.Where(category => proposed.All(next => next.Id != category.Id)))
        {
            db.Categories.Remove(removed);
        }

        return added;
    }

    private static string Describe(IEnumerable<Category> categories) =>
        string.Join(", ", categories.Select(category =>
            $"{category.Id}:{category.MinWeightKg.ToString(CultureInfo.InvariantCulture)}-" +
            $"{category.MaxWeightKg?.ToString(CultureInfo.InvariantCulture) ?? "up"}"));
}
