using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using VehicleManager.IntegrationTests.Support;

namespace VehicleManager.IntegrationTests;

// Runs as Production, where the framework answers some failures itself with an empty body unless told otherwise.
public class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory
        .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
        .CreateClient();

    [Theory]
    [InlineData("/api/nothing", null, HttpStatusCode.NotFound)]
    [InlineData("/api/vehicles", "not json", HttpStatusCode.BadRequest)]
    public async Task A_failure_the_framework_answers_itself_still_returns_a_problem_body(
        string url, string? json, HttpStatusCode expected)
    {
        var response = json is null
            ? await client.GetAsync(url)
            : await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(expected);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await response.Content.ReadAsStringAsync()).Should().Contain($"\"status\":{(int)expected}").And.NotContain("Exception");
    }
}
