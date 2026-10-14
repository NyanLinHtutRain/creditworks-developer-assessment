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

    [Fact]
    public void A_configuration_with_no_categories_is_rejected()
    {
        var result = CategoryConfigurationValidator.Validate(Array.Empty<Category>());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("At least one category must be defined");
    }

    [Fact]
    public void A_gap_between_two_bands_is_rejected()
    {
        var configuration = new List<Category>
        {
            Band("Light", 0m, 500m),
            Band("Medium", 600m, 2500m),
            Band("Heavy", 2500m, null)
        };

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("A gap exists between 500.00 kg and 600.00 kg");
    }

    [Fact]
    public void Overlapping_bands_are_rejected()
    {
        var configuration = new List<Category>
        {
            Band("Light", 0m, 600m),
            Band("Medium", 500m, 2500m),
            Band("Heavy", 2500m, null)
        };

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("'Light' and 'Medium' overlap between 500.00 kg and 600.00 kg");
    }

    [Fact]
    public void A_configuration_that_does_not_start_at_zero_is_rejected()
    {
        var configuration = Default();
        configuration[0].MinWeightKg = 100m;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("must start at 0.00 kg").And.Contain("starts at 100.00 kg");
    }

    [Fact]
    public void A_configuration_with_no_open_ended_band_is_rejected()
    {
        var configuration = Default();
        configuration[2].MaxWeightKg = 10000m;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("must be open-ended");
    }

    [Fact]
    public void A_configuration_with_two_open_ended_bands_is_rejected()
    {
        var configuration = Default();
        configuration[1].MaxWeightKg = null;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Only one category can be open-ended");
    }

    [Fact]
    public void An_open_ended_band_that_is_not_the_highest_is_rejected()
    {
        var configuration = new List<Category>
        {
            Band("Light", 0m, 500m),
            Band("Medium", 500m, null),
            Band("Heavy", 2500m, 5000m)
        };

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("open-ended category 'Medium' must be the highest");
    }

    [Fact]
    public void A_bound_with_more_than_two_decimal_places_is_rejected()
    {
        var configuration = Default();
        configuration[0].MaxWeightKg = Kg("500.001");
        configuration[1].MinWeightKg = Kg("500.001");

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().OnlyContain(error => error.Contains("at most 2 decimal places"));
    }

    [Fact]
    public void A_bound_written_with_trailing_zeros_is_accepted()
    {
        var configuration = Default();
        configuration[0].MaxWeightKg = Kg("500.000");
        configuration[1].MinWeightKg = Kg("500.000");

        CategoryConfigurationValidator.Validate(configuration).IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_bound_beyond_what_the_database_can_store_is_rejected()
    {
        var configuration = Default();
        configuration[2].MinWeightKg = CategoryRules.MaxWeightKg + 1m;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("cannot exceed 999999.99 kg"));
    }

    [Fact]
    public void A_negative_lower_bound_is_rejected()
    {
        var configuration = Default();
        configuration[0].MinWeightKg = -10m;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("cannot be negative"));
    }

    [Fact]
    public void An_upper_bound_below_its_lower_bound_is_rejected()
    {
        var configuration = Default();
        configuration[1].MaxWeightKg = 400m;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.Contains("must be greater than its lower bound of 500.00 kg"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_name_is_rejected(string name)
    {
        var configuration = Default();
        configuration[1].Name = name;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("A name is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_icon_is_rejected(string icon)
    {
        var configuration = Default();
        configuration[1].Icon = icon;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("An icon is required"));
    }

    [Theory]
    [InlineData("rocket")]
    [InlineData("CAR")]
    [InlineData("🚗")]
    public void An_icon_outside_the_allowed_set_is_rejected(string icon)
    {
        var configuration = Default();
        configuration[1].Icon = icon;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("is not recognised").And.Contain("delivery-truck");
    }

    [Fact]
    public void Every_allowed_icon_is_accepted()
    {
        foreach (var icon in CategoryIcons.Allowed)
        {
            var configuration = Default();
            configuration[1].Icon = icon;

            CategoryConfigurationValidator.Validate(configuration).IsValid
                .Should().BeTrue($"'{icon}' is an allowed icon");
        }
    }

    [Fact]
    public void A_name_that_differs_only_by_case_is_rejected_as_a_duplicate()
    {
        var configuration = Default();
        configuration[2].Name = "light";

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("used more than once"));
    }

    [Fact]
    public void A_name_that_differs_only_by_surrounding_spaces_is_rejected_as_a_duplicate()
    {
        var configuration = Default();
        configuration[2].Name = "  light  ";

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("used more than once"));
    }

    [Fact]
    public void An_overlong_name_is_rejected()
    {
        var configuration = Default();
        configuration[1].Name = new string('x', CategoryRules.MaxNameLength + 1);

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("characters or fewer"));
    }

    [Fact]
    public void Every_problem_is_reported_rather_than_only_the_first()
    {
        var configuration = Default();
        configuration[1].Name = string.Empty;
        configuration[1].Icon = string.Empty;

        var result = CategoryConfigurationValidator.Validate(configuration);

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validation_does_not_depend_on_the_order_categories_arrive_in()
    {
        var shuffled = Default().OrderByDescending(category => category.MinWeightKg).ToList();

        CategoryConfigurationValidator.Validate(shuffled).IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_valid_configuration_is_returned_in_ascending_weight_order()
    {
        var shuffled = Default().OrderByDescending(category => category.MinWeightKg).ToList();

        CategoryConfigurationValidator.Validate(shuffled).Categories
            .Select(category => category.Name).Should().ContainInOrder("Light", "Medium", "Heavy");
    }

    [Fact]
    public void An_invalid_configuration_returns_no_categories()
    {
        var result = CategoryConfigurationValidator.Validate(new[] { Band("Only", 100m, null) });

        result.IsValid.Should().BeFalse();
        result.Categories.Should().BeEmpty("a rejected configuration must not be offered for use");
    }
}
