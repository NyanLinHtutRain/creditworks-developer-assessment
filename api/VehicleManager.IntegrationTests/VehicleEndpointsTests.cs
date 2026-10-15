using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using VehicleManager.Api.Contracts;
using VehicleManager.IntegrationTests.Support;

namespace VehicleManager.IntegrationTests;

public class VehicleEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lists_the_five_seeded_manufacturers()
    {
        var manufacturers = await client.GetFromJsonAsync<List<ManufacturerResponse>>("/api/manufacturers");

        manufacturers!.Select(manufacturer => manufacturer.Name)
            .Should().Equal("Ferrari", "Honda", "Mazda", "Mercedes", "Toyota");
    }

    [Fact]
    public async Task Creating_a_vehicle_returns_it_with_its_manufacturer_and_category()
    {
        var response = await client.PostAsJsonAsync(
            "/api/vehicles", new VehicleRequest("John Smith", await ManufacturerIdAsync("Mazda"), 2019, 310.50m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var vehicle = await response.Content.ReadFromJsonAsync<VehicleResponse>();
        vehicle!.ManufacturerName.Should().Be("Mazda");
        vehicle.WeightKg.Should().Be(310.50m);
        vehicle.Category.Should().BeEquivalentTo(new { Name = "Light", Icon = "car" });
    }

    [Theory]
    [InlineData("owner", "asc", "Alice,Bob,Charlie")]
    [InlineData("owner", "desc", "Charlie,Bob,Alice")]
    [InlineData("manufacturer", "asc", "Bob,Charlie,Alice")]
    [InlineData("year", "asc", "Alice,Charlie,Bob")]
    [InlineData("weight", "desc", "Bob,Alice,Charlie")]
    public async Task The_list_sorts_by_the_chosen_field_and_resolves_each_category(
        string sortBy, string direction, string expectedOwners)
    {
        await CreateAsync("Charlie", "Mazda", 2019, 310.50m);
        await CreateAsync("Alice", "Toyota", 2015, 1850.75m);
        await CreateAsync("Bob", "Ferrari", 2021, 3900m);

        var vehicles = await client.GetFromJsonAsync<List<VehicleResponse>>(
            $"/api/vehicles?sortBy={sortBy}&direction={direction}");

        vehicles!.Select(vehicle => vehicle.OwnerName).Should().Equal(expectedOwners.Split(','));

        var categoryByOwner = new Dictionary<string, string> { ["Charlie"] = "Light", ["Alice"] = "Medium", ["Bob"] = "Heavy" };
        vehicles.Should().OnlyContain(vehicle => vehicle.Category!.Name == categoryByOwner[vehicle.OwnerName]);
    }

    // The specification's example: a 2200 kg vehicle is Medium until Medium is shortened to end at 2000 kg.
    [Fact]
    public async Task A_vehicle_changes_category_when_a_category_boundary_moves()
    {
        await CreateAsync("John Smith", "Mazda", 2019, 2200m);
        (await ListVehiclesAsync()).Single().Category!.Name.Should().Be("Medium");

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        var medium = categories!.Single(category => category.Name == "Medium");

        var edit = await client.PutAsJsonAsync(
            $"/api/categories/{medium.Id}", new CategoryRequest("Medium", "van", 500m, 2000m));

        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListVehiclesAsync()).Single().Category!.Name.Should().Be("Heavy");
    }

    [Theory]
    [InlineData("""{"manufacturerId":1,"yearOfManufacture":2019,"weightKg":100}""", "Owner's name is required")]
    [InlineData("""{"ownerName":"John","manufacturerId":1,"yearOfManufacture":2019,"weightKg":0}""", "greater than zero")]
    [InlineData("""{"ownerName":"John","manufacturerId":1,"yearOfManufacture":2019,"weightKg":100.123}""", "at most 2 decimal places")]
    [InlineData("""{"ownerName":"John","manufacturerId":1,"yearOfManufacture":1800,"weightKg":100}""", "Year of manufacture must be between")]
    [InlineData("""{"ownerName":"John","manufacturerId":999999,"yearOfManufacture":2019,"weightKg":100}""", "manufacturer does not exist")]
    public async Task An_invalid_vehicle_is_rejected_with_a_readable_error_and_not_stored(string json, string expected)
    {
        var response = await client.PostAsync("/api/vehicles", new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(expected);
        (await ListVehiclesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task An_unknown_sort_field_is_rejected()
    {
        var response = await client.GetAsync("/api/vehicles?sortBy=colour");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("sortBy must be one of");
    }

    private async Task<int> ManufacturerIdAsync(string name)
    {
        var manufacturers = await client.GetFromJsonAsync<List<ManufacturerResponse>>("/api/manufacturers");
        return manufacturers!.Single(manufacturer => manufacturer.Name == name).Id;
    }

    private async Task CreateAsync(string owner, string manufacturer, int year, decimal weight)
    {
        var response = await client.PostAsJsonAsync(
            "/api/vehicles", new VehicleRequest(owner, await ManufacturerIdAsync(manufacturer), year, weight));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<List<VehicleResponse>> ListVehiclesAsync() =>
        (await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!;
}
