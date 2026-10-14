namespace VehicleManager.Domain.Categories;

public static class CategoryIcons
{
    // Keys are Carbon icon names; the frontend maps each one to a component, so adding a key also needs a mapping there.
    public static readonly IReadOnlyList<string> Allowed = ["car", "van", "delivery-truck", "bus"];

    public static bool IsAllowed(string icon) => Allowed.Contains(icon);
}
