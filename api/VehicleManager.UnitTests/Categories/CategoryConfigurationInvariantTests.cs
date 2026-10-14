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
        var accepted = 0;

        for (var step = 0; step < Steps; step++)
        {
            var before = Snapshot(configuration);
            var result = ApplyRandomChange(random, configuration, step);

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

            accepted++;

            CategoryConfigurationValidator.Validate(configuration).IsValid.Should().BeTrue();

            foreach (var weight in ProbeWeights(configuration))
            {
                configuration.Count(category => category.Includes(weight))
                    .Should().Be(1, $"{weight} kg must resolve to exactly one category (step {step}, seed {seed})");
            }
        }

        accepted.Should().BeGreaterThan(50, "the run should exercise real changes, not just rejections");
    }

    private static CategoryConfigurationResult ApplyRandomChange(Random random, List<Category> configuration, int step)
    {
        var target = configuration[random.Next(configuration.Count)];

        return random.Next(3) switch
        {
            0 => CategoryConfigurationChanges.Create(
                configuration, $"Category {step}", RandomIcon(random), RandomWeight(random)),
            1 => CategoryConfigurationChanges.Edit(
                configuration, target.Id, target.Name, target.Icon, RandomWeight(random)),
            _ => CategoryConfigurationChanges.Delete(configuration, target.Id)
        };
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
