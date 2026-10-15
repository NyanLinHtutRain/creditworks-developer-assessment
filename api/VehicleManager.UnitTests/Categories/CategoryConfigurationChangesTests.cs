using FluentAssertions;
using VehicleManager.Domain.Categories;
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

    // The specification's example: Medium ends at 2000 kg, so a 2200 kg vehicle becomes Heavy.
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
        var result = CategoryConfigurationChanges.Edit(Default(), MediumId, "  Mid-size  ", "  bus  ", 500m, 2500m);

        result.IsValid.Should().BeTrue();

        var edited = result.Categories.Single(category => category.Id == MediumId);
        edited.Name.Should().Be("Mid-size");
        edited.Icon.Should().Be("bus");
        edited.MinWeightKg.Should().Be(500m);
        edited.MaxWeightKg.Should().Be(2500m);
    }

    [Theory]
    [InlineData("default", LightId, "Light", "car", "100", "500", "lowest category and must start at 0.00 kg")]
    [InlineData("default", MediumId, "Medium", "van", "2600", "2500", "must be greater than the lower bound")]
    [InlineData("four", 2, "Medium", "van", "500", "6000", "leave no room for it")]
    [InlineData("default", HeavyId, "Heavy", "delivery-truck", "2500", "9000", "must stay open-ended")]
    [InlineData("default", MediumId, "Medium", "van", "500", null, "needs an upper bound")]
    [InlineData("default", MediumId, "Medium", "van", "0", "2500", "not above 'Light'")]
    [InlineData("default", 99, "Ghost", "car", "500", null, "no longer exists")]
    [InlineData("default", MediumId, "Medium", "van", "500.001", "2500", "at most 2 decimal places")]
    [InlineData("default", MediumId, "Medium", "van", "2500.004", "2500", "at most 2 decimal places")]
    [InlineData("default", MediumId, "Medium", "van", "500", "2000.005", "at most 2 decimal places")]
    [InlineData("default", HeavyId, "medium", "delivery-truck", "2500", null, "used more than once")]
    [InlineData("default", MediumId, null, null, "500", "2500", "A name is required")]
    [InlineData("default", MediumId, "Medium", "rocket", "500", "2500", "is not recognised")]
    public void An_invalid_edit_is_rejected_with_a_clear_message(
        string configuration, int id, string? name, string? icon, string min, string? max, string expectedMessage)
    {
        var result = CategoryConfigurationChanges.Edit(Configuration(configuration), id, name, icon, Kg(min), KgOrNull(max));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains(expectedMessage));
        result.Categories.Should().BeEmpty("a rejected change returns nothing to act on");
    }

    [Theory]
    [InlineData("5000", null, "Heavy", "2500", "5000")]
    [InlineData("0", "100", "Light", "100", "500")]
    [InlineData("500", "1000", "Medium", "1000", "2500")]
    [InlineData("1500", "2500", "Medium", "500", "1500")]
    public void A_new_category_takes_a_piece_of_the_neighbour_it_shares_an_edge_with(
        string min, string? max, string neighbour, string neighbourMin, string? neighbourMax)
    {
        var result = CategoryConfigurationChanges.Create(Default(), "New category", "bus", Kg(min), KgOrNull(max));

        result.IsValid.Should().BeTrue();
        result.Categories.Should().HaveCount(4);

        var created = result.Categories.Single(category => category.Name == "New category");
        created.MinWeightKg.Should().Be(Kg(min));
        created.MaxWeightKg.Should().Be(KgOrNull(max));
        created.Id.Should().Be(0, "an unsaved category has no identity yet");

        var shrunk = result.Categories.Single(category => category.Name == neighbour);
        shrunk.MinWeightKg.Should().Be(Kg(neighbourMin));
        shrunk.MaxWeightKg.Should().Be(KgOrNull(neighbourMax));
    }

    [Fact]
    public void The_first_category_created_covers_every_weight()
    {
        var result = CategoryConfigurationChanges.Create([], "All vehicles", "car", 0m, null);

        result.IsValid.Should().BeTrue();
        result.Categories.Should().ContainSingle();
        result.Categories[0].MinWeightKg.Should().Be(0m);
        result.Categories[0].MaxWeightKg.Should().BeNull();
    }

    [Theory]
    [InlineData("default", "Duplicate", "car", "500", "2500", "'Medium' already covers exactly that range")]
    [InlineData("default", "Duplicate", "car", "2500", null, "'Heavy' already covers exactly that range")]
    [InlineData("default", "Inner", "car", "3000", "5000", "must start or end where an existing one does")]
    [InlineData("default", "Wide", "car", "400", "600", "runs past the end of 'Light'")]
    [InlineData("default", "Open", "car", "1500", null, "runs past the end of 'Medium'")]
    [InlineData("default", "Backwards", "car", "1000", "800", "must be greater than the lower bound")]
    [InlineData("default", "Precise", "car", "1500.005", "2500", "at most 2 decimal places")]
    [InlineData("default", "Negative", "car", "-5", null, "No category covers -5.00 kg")]
    [InlineData("default", "Enormous", "car", "1000000", null, "cannot exceed 999999.99 kg")]
    [InlineData("empty", "Late start", "car", "100", null, "must start at 0.00 kg")]
    [InlineData("default", "light", "bus", "5000", null, "used more than once")]
    [InlineData("default", "   ", "   ", "5000", null, "A name is required")]
    [InlineData("default", null, null, "5000", null, "A name is required")]
    [InlineData("default", "Super heavy", "rocket", "5000", null, "is not recognised")]
    public void An_invalid_new_category_is_rejected_with_a_clear_message(
        string configuration, string? name, string? icon, string min, string? max, string expectedMessage)
    {
        var result = CategoryConfigurationChanges.Create(Configuration(configuration), name, icon, Kg(min), KgOrNull(max));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains(expectedMessage));
        result.Categories.Should().BeEmpty("a rejected change returns nothing to act on");
    }

    [Theory]
    [InlineData(MediumId, LightId, "0", "2500")]
    [InlineData(LightId, MediumId, "0", "2500")]
    [InlineData(HeavyId, MediumId, "500", null)]
    public void Deleting_a_category_hands_its_range_to_a_neighbour_and_every_weight_still_resolves(
        int deleteId, int neighbourId, string neighbourMin, string? neighbourMax)
    {
        var result = CategoryConfigurationChanges.Delete(Default(), deleteId);

        result.IsValid.Should().BeTrue();
        result.Categories.Should().HaveCount(2);

        var neighbour = result.Categories.Single(category => category.Id == neighbourId);
        neighbour.MinWeightKg.Should().Be(Kg(neighbourMin));
        neighbour.MaxWeightKg.Should().Be(KgOrNull(neighbourMax));

        foreach (var weight in new[] { Kg("0.01"), Kg("499.99"), Kg("500.00"), Kg("2500.00"), Kg("99999.99") })
        {
            result.Categories.Count(category => category.Includes(weight))
                .Should().Be(1, $"{weight} kg must still have exactly one category");
        }
    }

    [Theory]
    [InlineData("single", 7, "is the only category and cannot be deleted")]
    [InlineData("default", 99, "no longer exists")]
    public void An_invalid_delete_is_rejected_with_a_clear_message(string configuration, int id, string expectedMessage)
    {
        var result = CategoryConfigurationChanges.Delete(Configuration(configuration), id);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain(expectedMessage);
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
}
