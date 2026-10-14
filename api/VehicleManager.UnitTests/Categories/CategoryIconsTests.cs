using FluentAssertions;
using VehicleManager.Domain.Categories;

namespace VehicleManager.UnitTests.Categories;

public class CategoryIconsTests
{
    [Fact]
    public void Every_allowed_icon_fits_the_database_column()
    {
        CategoryIcons.Allowed.Should().OnlyContain(icon => icon.Length <= CategoryRules.MaxIconLength);
    }

    [Fact]
    public void Allowed_icons_are_unique()
    {
        CategoryIcons.Allowed.Should().OnlyHaveUniqueItems();
    }
}
