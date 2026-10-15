using FluentAssertions;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryConfigurationValidatorTests
{
    [Fact]
    public void The_shipped_configuration_is_valid()
    {
        var result = CategoryConfigurationValidator.Validate(Default());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("no categories", "At least one category must be defined")]
    [InlineData("gap", "A gap exists between 500.00 kg and 600.00 kg")]
    [InlineData("overlap", "'Light' and 'Medium' overlap between 500.00 kg and 600.00 kg")]
    [InlineData("does not start at zero", "must start at 0.00 kg")]
    [InlineData("no open-ended band", "must be open-ended")]
    [InlineData("two open-ended bands", "Only one category can be open-ended")]
    [InlineData("open-ended band not highest", "open-ended category 'Medium' must be the highest")]
    [InlineData("three decimal places", "at most 2 decimal places")]
    [InlineData("above database ceiling", "cannot exceed 999999.99 kg")]
    [InlineData("negative lower bound", "cannot be negative")]
    [InlineData("upper below lower", "must be greater than its lower bound of 500.00 kg")]
    [InlineData("blank name", "A name is required")]
    [InlineData("blank icon", "An icon is required")]
    [InlineData("unknown icon", "is not recognised")]
    [InlineData("icon in the wrong case", "is not recognised")]
    [InlineData("emoji icon", "is not recognised")]
    [InlineData("duplicate name", "used more than once")]
    [InlineData("duplicate name with spaces", "used more than once")]
    [InlineData("overlong name", "characters or fewer")]
    public void An_invalid_configuration_is_rejected_with_a_clear_message(string scenario, string expectedMessage)
    {
        var result = CategoryConfigurationValidator.Validate(Invalid(scenario));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains(expectedMessage));
        result.Categories.Should().BeEmpty("a rejected configuration must not be offered for use");
    }

    // 500.000 carries no more precision than 500.00.
    [Fact]
    public void A_bound_written_with_trailing_zeros_is_accepted()
    {
        var configuration = Default();
        configuration[0].MaxWeightKg = Kg("500.000");
        configuration[1].MinWeightKg = Kg("500.000");

        CategoryConfigurationValidator.Validate(configuration).IsValid.Should().BeTrue();
    }

    [Fact]
    public void The_allowed_icons_are_accepted_unique_and_fit_the_database_column()
    {
        CategoryIcons.Allowed.Should().OnlyHaveUniqueItems();
        CategoryIcons.Allowed.Should().OnlyContain(icon => icon.Length <= CategoryRules.MaxIconLength);

        foreach (var icon in CategoryIcons.Allowed)
        {
            var configuration = Default();
            configuration[1].Icon = icon;

            CategoryConfigurationValidator.Validate(configuration).IsValid
                .Should().BeTrue($"'{icon}' is an allowed icon");
        }
    }

    [Fact]
    public void Every_problem_is_reported_rather_than_only_the_first()
    {
        var configuration = Default();
        configuration[1].Name = string.Empty;
        configuration[1].Icon = string.Empty;

        CategoryConfigurationValidator.Validate(configuration).Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Input_order_is_irrelevant_and_a_valid_result_is_in_ascending_weight_order()
    {
        var shuffled = Default().OrderByDescending(category => category.MinWeightKg).ToList();

        var result = CategoryConfigurationValidator.Validate(shuffled);

        result.IsValid.Should().BeTrue();
        result.Categories.Select(category => category.Name).Should().ContainInOrder("Light", "Medium", "Heavy");
    }

    private static List<Category> Invalid(string scenario) => scenario switch
    {
        "no categories" => [],
        "gap" => [Band("Light", 0m, 500m), Band("Medium", 600m, 2500m), Band("Heavy", 2500m, null)],
        "overlap" => [Band("Light", 0m, 600m), Band("Medium", 500m, 2500m), Band("Heavy", 2500m, null)],
        "does not start at zero" => With(c => c[0].MinWeightKg = 100m),
        "no open-ended band" => With(c => c[2].MaxWeightKg = 10000m),
        "two open-ended bands" => With(c => c[1].MaxWeightKg = null),
        "open-ended band not highest" =>
            [Band("Light", 0m, 500m), Band("Medium", 500m, null), Band("Heavy", 2500m, 5000m)],
        "three decimal places" => With(c =>
        {
            c[0].MaxWeightKg = Kg("500.001");
            c[1].MinWeightKg = Kg("500.001");
        }),
        "above database ceiling" => With(c => c[2].MinWeightKg = CategoryRules.MaxWeightKg + 1m),
        "negative lower bound" => With(c => c[0].MinWeightKg = -10m),
        "upper below lower" => With(c => c[1].MaxWeightKg = 400m),
        "blank name" => With(c => c[1].Name = "   "),
        "blank icon" => With(c => c[1].Icon = "   "),
        "unknown icon" => With(c => c[1].Icon = "rocket"),
        "icon in the wrong case" => With(c => c[1].Icon = "CAR"),
        "emoji icon" => With(c => c[1].Icon = "🚗"),
        "duplicate name" => With(c => c[2].Name = "light"),
        "duplicate name with spaces" => With(c => c[2].Name = "  light  "),
        "overlong name" => With(c => c[1].Name = new string('x', CategoryRules.MaxNameLength + 1)),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario.")
    };

    private static List<Category> With(Action<List<Category>> change)
    {
        var configuration = Default();
        change(configuration);
        return configuration;
    }
}
