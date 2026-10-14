using FluentAssertions;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryConfigurationInvariantTests
{
    private const int Steps = 300;

    // Fixed seeds keep every run identical, so a failure can always be reproduced.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Random_changes_never_leave_an_inconsistent_configuration(int seed)
    {
        var random = new Random(seed);
        var configuration = Default();
        var nextId = 100;
        var acceptedByKind = new Dictionary<string, int> { ["create"] = 0, ["edit"] = 0, ["delete"] = 0 };

        for (var step = 0; step < Steps; step++)
        {
            var before = Snapshot(configuration);
            var (kind, result) = ApplyRandomChange(random, configuration, step);

            if (!result.IsValid)
            {
                result.Categories.Should().BeEmpty();
                Snapshot(configuration).Should().Be(before, "a rejected change must not alter the configuration");
                continue;
            }

            configuration = result.Categories.ToList();

            foreach (var category in configuration.Where(category => category.Id == 0))
            {
                category.Id = nextId++;
            }

            acceptedByKind[kind]++;

            CategoryConfigurationValidator.Validate(configuration).IsValid.Should().BeTrue();

            foreach (var weight in ProbeWeights(configuration))
            {
                configuration.Count(category => category.Includes(weight))
                    .Should().Be(1, $"{weight} kg must resolve to exactly one category (step {step}, seed {seed})");
            }
        }

        foreach (var (kind, count) in acceptedByKind)
        {
            count.Should().BeGreaterThan(5, $"the run should accept some {kind} changes, not only rejections");
        }
    }

    private static (string Kind, CategoryConfigurationResult Result) ApplyRandomChange(
        Random random, List<Category> configuration, int step)
    {
        var target = configuration[random.Next(configuration.Count)];

        return random.Next(3) switch
        {
            0 => ("create", RandomCreate(random, configuration, target, step)),
            1 => ("edit", RandomEdit(random, configuration, target)),
            _ => ("delete", CategoryConfigurationChanges.Delete(configuration, target.Id))
        };
    }

    // Half the time build a range that fits an existing category, so real changes happen; otherwise anything goes.
    private static CategoryConfigurationResult RandomCreate(
        Random random, List<Category> configuration, Category band, int step)
    {
        var name = $"Category {step}";
        var icon = RandomIcon(random);
        var cut = RandomBetween(random, band.MinWeightKg, band.MaxWeightKg);

        if (cut is { } point && random.Next(2) == 0)
        {
            return random.Next(2) == 0
                ? CategoryConfigurationChanges.Create(configuration, name, icon, band.MinWeightKg, point)
                : CategoryConfigurationChanges.Create(configuration, name, icon, point, band.MaxWeightKg);
        }

        decimal? max = random.Next(2) == 0 ? null : RandomWeight(random);
        return CategoryConfigurationChanges.Create(configuration, name, icon, RandomWeight(random), max);
    }

    // Half the time pick bounds that fit between the neighbours, so real edits happen; otherwise anything goes.
    private static CategoryConfigurationResult RandomEdit(Random random, List<Category> configuration, Category target)
    {
        var ordered = configuration.OrderBy(category => category.MinWeightKg).ToList();
        var index = ordered.IndexOf(target);
        var below = index > 0 ? ordered[index - 1] : null;
        var above = index < ordered.Count - 1 ? ordered[index + 1] : null;

        if (random.Next(2) == 0)
        {
            decimal? min = below is null ? 0m : RandomBetween(random, below.MinWeightKg, target.MaxWeightKg);
            decimal? max = above is null || min is null ? null : RandomBetween(random, min.Value, above.MaxWeightKg);

            if (min is { } newMin && (above is null || max is not null))
            {
                return CategoryConfigurationChanges.Edit(
                    configuration, target.Id, target.Name, target.Icon, newMin, max);
            }
        }

        decimal? anyMax = random.Next(2) == 0 ? null : RandomWeight(random);
        return CategoryConfigurationChanges.Edit(
            configuration, target.Id, target.Name, target.Icon, RandomWeight(random), anyMax);
    }

    // A weight strictly between two bounds, or null when there is no room.
    private static decimal? RandomBetween(Random random, decimal low, decimal? high)
    {
        var lowCents = (int)(low * 100) + 1;
        var highCents = (int)((high ?? 7000m) * 100) - 1;

        return lowCents > highCents ? null : random.Next(lowCents, highCents + 1) / 100m;
    }

    private static decimal RandomWeight(Random random) => random.Next(1, 600_000) / 100m;

    private static string RandomIcon(Random random) =>
        CategoryIcons.Allowed[random.Next(CategoryIcons.Allowed.Count)];

    // Every boundary and the weights either side of it are where an off-by-one would show up.
    private static IEnumerable<decimal> ProbeWeights(List<Category> configuration)
    {
        yield return 0m;
        yield return 0.01m;
        yield return CategoryRules.MaxWeightKg;

        foreach (var category in configuration.Where(category => category.MinWeightKg > 0))
        {
            yield return category.MinWeightKg - 0.01m;
            yield return category.MinWeightKg;
            yield return category.MinWeightKg + 0.01m;
        }
    }

    private static string Snapshot(List<Category> configuration) =>
        string.Join("|", configuration.Select(category =>
            $"{category.Id}:{category.Name}:{category.Icon}:{category.MinWeightKg}:{category.MaxWeightKg}"));
}
