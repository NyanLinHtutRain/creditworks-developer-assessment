using FluentAssertions;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryConfigurationChangesTests
{
    [Fact]
    public void Moving_a_start_moves_the_end_of_the_category_below()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "Heavy", "delivery-truck", 2000m, null);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Medium").MaxWeightKg.Should().Be(2000m);
        result.Categories.Single(category => category.Name == "Heavy").MinWeightKg.Should().Be(2000m);
    }

    [Fact]
    public void Moving_an_end_moves_the_start_of_the_category_above()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 500m, 2000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Medium").MaxWeightKg.Should().Be(2000m);
        result.Categories.Single(category => category.Name == "Heavy").MinWeightKg.Should().Be(2000m);
    }

    [Fact]
    public void Both_bounds_can_move_in_one_edit()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 800m, 2000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Light").MaxWeightKg.Should().Be(800m);
        result.Categories.Single(category => category.Name == "Heavy").MinWeightKg.Should().Be(2000m);
    }

    [Fact]
    public void An_existing_vehicle_changes_category_when_the_end_of_Medium_moves_to_2000()
    {
        var configuration = Default();
        var weight = Kg("2200.00");

        CategoryResolver.Resolve(configuration, weight)!.Name.Should().Be("Medium");

        var result = CategoryConfigurationChanges.Edit(configuration, MediumId, "Medium", "van", 500m, 2000m);

        CategoryResolver.Resolve(result.Categories, weight)!.Name.Should().Be("Heavy");
    }

    [Fact]
    public void A_category_can_be_renamed_and_re_iconed_without_touching_its_bounds()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Mid-size", "bus", 500m, 2500m);

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
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "  Mid-size  ", "  bus  ", 500m, 2500m);

        var edited = result.Categories.Single(category => category.Id == MediumId);
        edited.Name.Should().Be("Mid-size");
        edited.Icon.Should().Be("bus");
    }

    [Fact]
    public void The_lowest_category_cannot_be_moved_off_zero()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Light", "car", 100m, 500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("lowest category and must start at 0.00 kg");
    }

    [Fact]
    public void The_lowest_category_can_still_be_renamed()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Tiny", "bus", 0m, 500m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == LightId).Name.Should().Be("Tiny");
    }

    [Fact]
    public void A_start_at_or_above_the_end_of_the_same_category_is_rejected()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 2600m, 2500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("must be greater than the lower bound");
    }

    [Fact]
    public void An_end_cannot_be_moved_past_the_end_of_the_category_above()
    {
        var configuration = new List<Category>
        {
            Band("Light", 0m, 500m, id: 1),
            Band("Medium", 500m, 2500m, id: 2),
            Band("Heavy", 2500m, 5000m, id: 3),
            Band("Super heavy", 5000m, null, id: 4)
        };

        var result = CategoryConfigurationChanges.Edit(configuration, 2, "Medium", "van", 500m, 6000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("leave no room for it");
    }

    [Fact]
    public void The_highest_category_cannot_be_given_an_upper_bound()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "Heavy", "delivery-truck", 2500m, 9000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("must stay open-ended");
    }

    [Fact]
    public void A_category_below_the_top_cannot_be_made_open_ended()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 500m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("needs an upper bound");
    }

    [Fact]
    public void A_start_cannot_be_moved_onto_the_start_of_the_category_below()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "van", 0m, 2500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("not above 'Light'");
    }

    [Fact]
    public void The_highest_category_can_be_moved_up_with_no_upper_neighbour_to_respect()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "Heavy", "delivery-truck", 3000m, null);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Id == MediumId).MaxWeightKg.Should().Be(3000m);
        result.Categories.Single(category => category.Id == HeavyId).MaxWeightKg.Should().BeNull();
    }

    [Fact]
    public void Editing_a_category_that_is_gone_is_rejected()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), 99, "Ghost", "car", 500m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("no longer exists");
    }

    [Theory]
    [InlineData("500.001", "2500")]
    [InlineData("2500.004", "2500")]
    [InlineData("500", "2000.005")]
    public void An_edit_with_more_than_two_decimal_places_is_reported_as_a_precision_problem(
        string minWeight, string maxWeight)
    {
        var result = CategoryConfigurationChanges.Edit(
            Default(), MediumId, "Medium", "van", Kg(minWeight), Kg(maxWeight));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("at most 2 decimal places");
    }

    [Fact]
    public void Creating_a_category_with_an_existing_name_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "light", "bus", 5000m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("used more than once");
    }

    [Fact]
    public void Renaming_a_category_to_an_existing_name_is_rejected()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), HeavyId, "medium", "delivery-truck", 2500m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("used more than once");
    }

    [Fact]
    public void A_null_name_or_icon_is_rejected_on_create_rather_than_throwing()
    {
        var result = CategoryConfigurationChanges.Create(Default(), null, null, 5000m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("A name is required"));
        result.Errors.Should().Contain(error => error.Contains("An icon is required"));
    }

    [Fact]
    public void A_null_name_or_icon_is_rejected_on_edit_rather_than_throwing()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, null, null, 500m, 2500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("A name is required"));
        result.Errors.Should().Contain(error => error.Contains("An icon is required"));
    }

    [Fact]
    public void Creating_a_category_above_the_top_takes_the_open_ended_end()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Super heavy", "bus", 5000m, null);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Heavy").MaxWeightKg.Should().Be(5000m);

        var created = result.Categories.Single(category => category.Name == "Super heavy");
        created.MinWeightKg.Should().Be(5000m);
        created.MaxWeightKg.Should().BeNull();
        created.Id.Should().Be(0, "an unsaved category has no identity yet");
    }

    [Fact]
    public void Creating_a_category_below_the_bottom_takes_the_lower_piece()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Extra light", "bus", 0m, 100m);

        result.IsValid.Should().BeTrue();

        var created = result.Categories.Single(category => category.Name == "Extra light");
        created.MinWeightKg.Should().Be(0m);
        created.MaxWeightKg.Should().Be(100m);

        var light = result.Categories.Single(category => category.Name == "Light");
        light.MinWeightKg.Should().Be(100m);
        light.MaxWeightKg.Should().Be(500m);
    }

    [Fact]
    public void Creating_a_category_at_the_start_of_a_middle_band_takes_the_lower_piece()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Lower medium", "van", 500m, 1000m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Lower medium").MaxWeightKg.Should().Be(1000m);

        var medium = result.Categories.Single(category => category.Name == "Medium");
        medium.MinWeightKg.Should().Be(1000m);
        medium.MaxWeightKg.Should().Be(2500m);
    }

    [Fact]
    public void Creating_a_category_at_the_end_of_a_closed_band_takes_the_upper_piece()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Midweight", "van", 1500m, 2500m);

        result.IsValid.Should().BeTrue();
        result.Categories.Single(category => category.Name == "Medium").MaxWeightKg.Should().Be(1500m);

        var created = result.Categories.Single(category => category.Name == "Midweight");
        created.MinWeightKg.Should().Be(1500m);
        created.MaxWeightKg.Should().Be(2500m);
    }

    [Fact]
    public void The_first_category_created_covers_every_weight()
    {
        var result = CategoryConfigurationChanges.Create(Array.Empty<Category>(), "All vehicles", "car", 0m, null);

        result.IsValid.Should().BeTrue();
        result.Categories.Should().ContainSingle();
        result.Categories[0].MinWeightKg.Should().Be(0m);
        result.Categories[0].MaxWeightKg.Should().BeNull();
    }

    [Fact]
    public void The_first_category_created_must_still_start_at_zero()
    {
        var result = CategoryConfigurationChanges.Create(Array.Empty<Category>(), "Late start", "car", 100m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("must start at 0.00 kg");
    }

    [Fact]
    public void A_range_that_matches_a_closed_category_exactly_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Duplicate", "car", 500m, 2500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("'Medium' already covers exactly that range");
    }

    [Fact]
    public void A_range_that_matches_the_open_ended_category_exactly_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Duplicate", "car", 2500m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("'Heavy' already covers exactly that range");
    }

    [Fact]
    public void A_range_inside_a_category_that_touches_neither_end_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Inner", "car", 3000m, 5000m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("must start or end where an existing one does").And.Contain("'Heavy'");
    }

    [Fact]
    public void A_range_that_runs_past_the_end_of_a_category_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Wide", "car", 400m, 600m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("runs past the end of 'Light'");
    }

    [Fact]
    public void A_closed_range_cannot_be_created_inside_a_closed_category_without_an_upper_bound()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Open", "car", 1500m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("runs past the end of 'Medium'");
    }

    [Fact]
    public void A_range_with_an_upper_bound_at_or_below_its_lower_bound_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Backwards", "car", 1000m, 800m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("must be greater than the lower bound");
    }

    [Fact]
    public void A_range_with_more_than_two_decimal_places_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Precise", "car", Kg("1500.005"), 2500m);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("at most 2 decimal places");
    }

    [Fact]
    public void A_range_starting_outside_the_covered_weights_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Negative", "car", -5m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("No category covers -5.00 kg");
    }

    [Fact]
    public void A_range_the_database_cannot_store_is_rejected()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Enormous", "car", 1_000_000m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("cannot exceed 999999.99 kg"));
    }

    [Fact]
    public void A_created_category_still_has_to_pass_validation()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "   ", "   ", 5000m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("A name is required"));
        result.Errors.Should().Contain(error => error.Contains("An icon is required"));
    }

    [Fact]
    public void An_unrecognised_icon_is_rejected_when_creating()
    {
        var result = CategoryConfigurationChanges.Create(Default(), "Super heavy", "rocket", 5000m, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("is not recognised");
    }

    [Fact]
    public void An_unrecognised_icon_is_rejected_when_editing()
    {
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "Medium", "rocket", 500m, 2500m);

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

        CategoryConfigurationChanges.Edit(configuration, HeavyId, "Heavy", "delivery-truck", 2000m, null);
        CategoryConfigurationChanges.Create(configuration, "Super heavy", "bus", 5000m, null);
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
        var result = CategoryConfigurationChanges.Edit(Default(), LightId, "Light", "car", 100m, 500m);

        result.Categories.Should().BeEmpty();
    }
}
