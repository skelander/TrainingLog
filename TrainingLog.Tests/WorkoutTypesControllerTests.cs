using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TrainingLog.Tests;

public class WorkoutTypesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WorkoutTypesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("/workout-types");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithUserToken_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).GetAsync("/workout-types");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsSeedData()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var types = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        Assert.NotNull(types);
        Assert.Contains(types, t => t.Name == "Running");
        Assert.Contains(types, t => t.Name == "BJJ");
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreated()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workout-types", new
        {
            Name = "Swimming",
            Fields = new[] { new { Name = "Laps", Type = 0, Unit = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_AsUser_Returns403()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workout-types", new
        {
            Name = "Swimming",
            Fields = new[] { new { Name = "Laps", Type = 0, Unit = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(token).PostAsJsonAsync("/workout-types", new
        {
            Name = "ToDelete",
            Fields = Array.Empty<object>()
        });
        var body = await created.Content.ReadFromJsonAsync<WorkoutTypeResponse>();

        var del = await _client.WithToken(token).DeleteAsync($"/workout-types/{body!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).DeleteAsync("/workout-types/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private record WorkoutTypeResponse(int Id, string Name, List<object> Fields);
}
