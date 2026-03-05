using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TrainingLog.Tests;

public class WorkoutsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WorkoutsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private object RunningSession(int workoutTypeId = 1) => new
    {
        WorkoutTypeId = workoutTypeId,
        LoggedAt = DateTime.UtcNow,
        Notes = (string?)null,
        Values = new[] { new { FieldDefinitionId = 1, Value = "5" } }
    };

    [Fact]
    public async Task GetMine_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("/workouts");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetMine_WithToken_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).GetAsync("/workouts");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Create_ValidSession_ReturnsCreated()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workouts", RunningSession());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidWorkoutType_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workouts", RunningSession(workoutTypeId: 99999));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetMine_ContainsNewSession()
    {
        var token = await Helpers.GetTokenAsync(_client, "bob", "bob");
        await _client.WithToken(token).PostAsJsonAsync("/workouts", RunningSession());

        var sessions = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutSessionResponse>>("/workouts");
        Assert.NotNull(sessions);
        Assert.NotEmpty(sessions);
    }

    [Fact]
    public async Task GetById_OtherUsersSession_Returns403()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", RunningSession());
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(adminToken).GetAsync($"/workouts/{session!.Id}");
        // Admin can see any session
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bobToken = await Helpers.GetTokenAsync(_client, "bob", "bob");
        var bobRes = await _client.WithToken(bobToken).GetAsync($"/workouts/{session.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, bobRes.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnSession_ReturnsNoContent()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var created = await _client.WithToken(token).PostAsJsonAsync("/workouts", RunningSession());
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var del = await _client.WithToken(token).DeleteAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_OtherUsersSession_Returns403()
    {
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var bobToken = await Helpers.GetTokenAsync(_client, "bob", "bob");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", RunningSession());
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(bobToken).DeleteAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private record WorkoutSessionResponse(int Id, string Username);
}
