using FluentAssertions;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryConfigurationChangesTests
{
    [Fact]
    public void Moving_a_boundary_moves_the_neighbouring_bound_with_it()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "Heavy", "delivery-truck", 2000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Medium").MaxWeightKg.Should().Be(2000m);
        result.Categories.Single(category => category.Name == "Heavy").MinWeightKg.Should().Be(2000m);
    }

    [Fact]
    public void An_existing_vehicle_changes_category_when_the_boundary_moves()
    {
        var configuration = Default();
        var weight = Kg("2200.00");

        CategoryResolver.Resolve(configuration, weight)!.Name.Should().Be("Medium");

        var result = CategoryConfigurationChanges.Edit(configuration, HeavyId, "Heavy", "delivery-truck", 2000m);

        CategoryResolver.Resolve(result.Categories, weight)!.Name.Should().Be("Heavy");
    }

    [Fact]
    public void A_category_can_be_renamed_and_re_iconed_without_touching_its_bounds()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Mid-size", "bus", 500m);

        result.IsValid.Should().BeTrue();

        var edited = result.Categories.Single(category => category.Id == MediumId);
        edited.Name.Should().Be("Mid-size");
        edited.Icon.Should().Be("bus");
        edited.MinWeightKg.Should().Be(500m);
        edited.MaxWeightKg.Should().Be(2500m);
    }

    [Fact]
    public void Names_and_icons_are_trimmed()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "  Mid-size  ", "  bus  ", 500m);

        var edited = result.Categories.Single(category => category.Id == MediumId);
        edited.Name.Should().Be("Mid-size");
        edited.Icon.Should().Be("bus");
    }

    [Fact]
    public void The_lowest_category_cannot_be_moved_off_zero()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Light", "car", 100m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("lowest category and must start at 0.00 kg");
    }

    [Fact]
    public void The_lowest_category_can_still_be_renamed()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Tiny", "bus", 0m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == LightId).Name.Should().Be("Tiny");
    }

    [Fact]
    public void A_boundary_cannot_be_moved_past_the_category_above_it()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 2600m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("not below 'Heavy'");
    }

    [Fact]
    public void A_boundary_cannot_be_moved_onto_the_category_below_it()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 0m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("not above 'Light'");
    }

    [Fact]
    public void The_highest_category_can_be_moved_up_with_no_upper_neighbour_to_respect()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "Heavy", "delivery-truck", 3000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == MediumId).MaxWeightKg.Should().Be(3000m);
        result.Categories.Single(category => category.Id == HeavyId).MaxWeightKg.Should().BeNull();
    }

    [Fact]
    public void Editing_a_category_that_is_gone_is_rejected()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), 99, "Ghost", "car", 500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("no longer exists");
    }

    [Fact]
    public void An_edit_that_would_break_a_bound_is_rejected_by_the_validator()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", Kg("500.001"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("at most 2 decimal places"));
    }

    [Fact]
    public void Creating_a_category_splits_the_band_that_contains_the_split_point()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Super heavy", "bus", 5000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Heavy").MaxWeightKg.Should().Be(5000m);

        var created = result.Categories.Single(category => category.Name == "Super heavy");
        created.MinWeightKg.Should().Be(5000m);
        created.MaxWeightKg.Should().BeNull();
        created.Id.Should().Be(0, "an unsaved category has no identity yet");
    }

    [Fact]
    public void Creating_a_category_inside_a_closed_band_takes_that_bands_upper_bound()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Midweight", "van", 1500m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Medium").MaxWeightKg.Should().Be(1500m);

        var created = result.Categories.Single(category => category.Name == "Midweight");
        created.MinWeightKg.Should().Be(1500m);
        created.MaxWeightKg.Should().Be(2500m);
    }

    [Fact]
    public void The_first_category_created_covers_every_weight()
    {
        var result = CategoryConfigurationChanges.Create(Array.Empty<Category>(), "All vehicles", "car", 0m);

        result.IsValid.Should().BeTrue();
        result.Categories.Should().ContainSingle();
        result.Categories[0].MinWeightKg.Should().Be(0m);
        result.Categories[0].MaxWeightKg.Should().BeNull();
    }

    [Theory]
    [InlineData("0.00", "Light")]
    [InlineData("500.00", "Medium")]
    [InlineData("2500.00", "Heavy")]
    public void A_split_point_on_an_existing_edge_is_rejected(string splitPoint, string existingCategory)
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Duplicate", "car", Kg(splitPoint));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain($"'{existingCategory}' already starts at");
    }

    [Fact]
    public void A_split_point_outside_the_covered_range_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Negative", "car", -5m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("No category covers -5.00 kg");
    }

    [Fact]
    public void A_split_point_the_database_cannot_store_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Enormous", "car", 1_000_000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("cannot exceed 999999.99 kg"));
    }

    [Fact]
    public void A_created_category_still_has_to_pass_validation()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "   ", "   ", 5000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("A name is required"));
        result.Errors.Should().Contain(error => error.Contains("An icon is required"));
    }

    [Fact]
    public void An_unrecognised_icon_is_rejected_when_creating()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Super heavy", "rocket", 5000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("is not recognised");
    }

    [Fact]
    public void An_unrecognised_icon_is_rejected_when_editing()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "rocket", 500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("is not recognised");
    }

    [Fact]
    public void Deleting_a_middle_category_hands_its_range_to_the_one_below()
    {
        var result = CategoryConfigurationChanges.Delete(Default(), MediumId);

        result.IsValid.Should().BeTrue();
        result.Categories.Should().HaveCount(2);
        result.Categories.Single(category => category.Id == LightId).MaxWeightKg.Should().Be(2500m);
        result.Categories.Single(category => category.Id == HeavyId).MinWeightKg.Should().Be(2500m);
    }

    [Fact]
    public void Deleting_the_lowest_category_pulls_the_next_one_down_to_zero()
    {
        var result = CategoryConfigurationChanges.Delete(Default(), LightId);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == MediumId).MinWeightKg.Should().Be(0m);
    }

    [Fact]
    public void Deleting_the_highest_category_leaves_the_one_below_open_ended()
    {
        var result = CategoryConfigurationChanges.Delete(Default(), HeavyId);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == MediumId).MaxWeightKg
            .Should().BeNull("something must still cover the heaviest vehicles");
    }

    [Fact]
    public void Every_weight_still_resolves_after_a_deletion()
    {
        var result = CategoryConfigurationChanges.Delete(Default(), MediumId);

        foreach (var weight in new[] { Kg("0.01"), Kg("499.99"), Kg("500.00"), Kg("2500.00"), Kg("99999.99") })
        {
            result.Categories.Count(category => category.Includes(weight))
                .Should().Be(1, $"{weight} kg must still have exactly one category");
        }
    }

    [Fact]
    public void The_last_remaining_category_cannot_be_deleted()
    {
        var result = CategoryConfigurationChanges.Delete([Band("Everything", 0m, null, id: 7)], 7);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("is the only category and cannot be deleted");
    }

    [Fact]
    public void Deleting_a_category_that_is_gone_is_rejected()
    {
        var result = CategoryConfigurationChanges.Delete(Default(), 99);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("no longer exists");
    }

    [Fact]
    public void A_successful_change_leaves_the_categories_it_was_given_untouched()
    {
        var configuration = Default();

        CategoryConfigurationChanges.Edit(configuration, HeavyId, "Heavy", "delivery-truck", 2000m);
        CategoryConfigurationChanges.Create(configuration, "Super heavy", "bus", 5000m);
        CategoryConfigurationChanges.Delete(configuration, MediumId);

        configuration.Should().HaveCount(3);
        configuration[0].MaxWeightKg.Should().Be(500m);
        configuration[1].MinWeightKg.Should().Be(500m);
        configuration[1].MaxWeightKg.Should().Be(2500m);
        configuration[2].MinWeightKg.Should().Be(2500m);
        configuration[2].MaxWeightKg.Should().BeNull();
    }

    [Fact]
    public void A_rejected_change_returns_no_categories_to_act_on()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Light", "car", 100m);

        result.Categories.Should().BeEmpty();
    }
}
