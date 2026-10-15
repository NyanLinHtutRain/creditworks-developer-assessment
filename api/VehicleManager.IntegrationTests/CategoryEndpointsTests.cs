using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using VehicleManager.Api.Contracts;
using VehicleManager.IntegrationTests.Support;

namespace VehicleManager.IntegrationTests;

public class CategoryEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lists_the_categories_in_weight_order()
    {
        var categories = await ListAsync();

        categories.Select(category => category.Name).Should().Equal("Light", "Medium", "Heavy");
        categories[2].MaxWeightKg.Should().BeNull();
    }

    [Fact]
    public async Task Creating_a_category_from_a_range_shrinks_its_neighbour()
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Extra light", "bus", 0m, 100m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categories = await ListAsync();
        categories.Select(category => category.Name).Should().Equal("Extra light", "Light", "Medium", "Heavy");
        categories[0].MaxWeightKg.Should().Be(100m);
        categories[1].MinWeightKg.Should().Be(100m);
    }

    [Theory]
    [InlineData("""{"name":"Inner","icon":"car","minWeightKg":3000,"maxWeightKg":5000}""", "must start or end where an existing one does")]
    [InlineData("""{"name":"No start","icon":"car"}""", "starting weight is required")]
    public async Task An_invalid_category_is_rejected_with_a_readable_error_and_changes_nothing(string json, string expected)
    {
        var response = await client.PostAsync("/api/categories", new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(expected);
        (await ListAsync()).Should().HaveCount(3);
    }

    [Fact]
    public async Task Editing_the_end_of_Medium_moves_the_start_of_Heavy()
    {
        var medium = (await ListAsync()).Single(category => category.Name == "Medium");

        var response = await client.PutAsJsonAsync(
            $"/api/categories/{medium.Id}", new CategoryRequest("Medium", "van", 500m, 2000m));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync()).Single(category => category.Name == "Heavy").MinWeightKg.Should().Be(2000m);
    }

    [Fact]
    public async Task Editing_or_deleting_an_unknown_category_returns_404()
    {
        var edit = await client.PutAsJsonAsync("/api/categories/999999", new CategoryRequest("Ghost", "car", 0m, 100m));
        var delete = await client.DeleteAsync("/api/categories/999999");

        edit.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleting_a_category_hands_its_range_to_the_one_below()
    {
        var medium = (await ListAsync()).Single(category => category.Name == "Medium");

        var response = await client.DeleteAsync($"/api/categories/{medium.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var categories = await ListAsync();
        categories.Select(category => category.Name).Should().Equal("Light", "Heavy");
        categories[0].MaxWeightKg.Should().Be(2500m);
    }

    [Fact]
    public async Task The_last_remaining_category_cannot_be_deleted()
    {
        foreach (var name in new[] { "Medium", "Heavy" })
        {
            var category = (await ListAsync()).Single(candidate => candidate.Name == name);
            (await client.DeleteAsync($"/api/categories/{category.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var last = (await ListAsync()).Single();
        var response = await client.DeleteAsync($"/api/categories/{last.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("is the only category");
    }

    [Fact]
    public async Task An_unreadable_request_returns_400_without_technical_details()
    {
        var response = await client.PostAsync("/api/categories", new StringContent("not json", Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("The request could not be read.").And.NotContain("Exception");
    }

    private async Task<List<CategoryResponse>> ListAsync() =>
        (await client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories"))!;
}
