using FluentAssertions;
using VehicleManager.Domain.Vehicles;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Vehicles;

public class VehicleValidatorTests
{
    private const int CurrentYear = 2026;

    [Fact]
    public void A_complete_vehicle_is_valid_including_the_edge_years_and_weights()
    {
        VehicleValidator.Validate("John Smith", 1, 2019, 1850.75m, CurrentYear).Should().BeEmpty();
        VehicleValidator.Validate("John Smith", 1, VehicleRules.MinYear, 0.01m, CurrentYear).Should().BeEmpty();
        VehicleValidator.Validate("John Smith", 1, CurrentYear + 1, 999999.99m, CurrentYear).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, 1, 2019, "1850.75", "Owner's name is required")]
    [InlineData("   ", 1, 2019, "1850.75", "Owner's name is required")]
    [InlineData("John", null, 2019, "1850.75", "A manufacturer is required")]
    [InlineData("John", 1, null, "1850.75", "Year of manufacture is required")]
    [InlineData("John", 1, 1885, "1850.75", "must be between 1886 and 2027")]
    [InlineData("John", 1, 2028, "1850.75", "must be between 1886 and 2027")]
    [InlineData("John", 1, 2019, null, "Weight is required")]
    [InlineData("John", 1, 2019, "0", "Weight must be greater than zero")]
    [InlineData("John", 1, 2019, "-5", "Weight must be greater than zero")]
    [InlineData("John", 1, 2019, "1000000", "cannot exceed 999999.99 kg")]
    [InlineData("John", 1, 2019, "1850.755", "at most 2 decimal places")]
    public void An_invalid_vehicle_is_rejected_with_a_clear_message(
        string? owner, int? manufacturerId, int? year, string? weight, string expectedMessage)
    {
        var errors = VehicleValidator.Validate(owner, manufacturerId, year, KgOrNull(weight), CurrentYear);

        errors.Should().Contain(error => error.Contains(expectedMessage));
    }

    [Fact]
    public void An_overlong_owner_name_is_rejected()
    {
        var owner = new string('x', VehicleRules.MaxOwnerNameLength + 1);

        VehicleValidator.Validate(owner, 1, 2019, 1850.75m, CurrentYear)
            .Should().ContainSingle().Which.Should().Contain("100 characters or fewer");
    }
}
