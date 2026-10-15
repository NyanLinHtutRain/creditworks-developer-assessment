using FluentAssertions;
using VehicleManager.Domain.Categories;
using VehicleManager.Domain.Entities;
using static VehicleManager.UnitTests.Categories.TestCategories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryResolverTests
{
    // Zero is covered because the lowest band's lower bound is inclusive; rejecting it is vehicle validation's job.
    [Theory]
    [InlineData("0.00", "Light")]
    [InlineData("0.01", "Light")]
    [InlineData("499.99", "Light")]
    [InlineData("500.00", "Medium")]
    [InlineData("500.01", "Medium")]
    [InlineData("1850.75", "Medium")]
    [InlineData("2499.99", "Medium")]
    [InlineData("2500.00", "Heavy")]
    [InlineData("2500.01", "Heavy")]
    [InlineData("999999.99", "Heavy")]
    public void Resolve_maps_a_weight_to_the_band_that_covers_it(string weight, string expectedCategory)
    {
        var resolved = CategoryResolver.Resolve(Default(), Kg(weight));

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be(expectedCategory);
    }

    [Fact]
    public void A_weight_on_a_boundary_belongs_to_exactly_one_category()
    {
        var configuration = Default();

        foreach (var boundary in new[] { Kg("0.00"), Kg("500.00"), Kg("2500.00") })
        {
            configuration.Count(category => category.Includes(boundary))
                .Should().Be(1, $"{boundary} kg sits on a boundary and must have one owner");
        }
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-1000.00")]
    public void Resolve_returns_nothing_for_a_negative_weight(string weight)
    {
        CategoryResolver.Resolve(Default(), Kg(weight)).Should().BeNull();
    }

    [Fact]
    public void Resolve_returns_nothing_when_no_categories_are_configured()
    {
        CategoryResolver.Resolve(Array.Empty<Category>(), Kg("1850.75")).Should().BeNull();
    }

    [Fact]
    public void Resolve_is_unaffected_by_the_order_categories_arrive_in()
    {
        var shuffled = Default().OrderByDescending(category => category.MinWeightKg).ToList();

        CategoryResolver.Resolve(shuffled, Kg("500.00"))!.Name.Should().Be("Medium");
        CategoryResolver.Resolve(shuffled, Kg("12.34"))!.Name.Should().Be("Light");
    }
}
