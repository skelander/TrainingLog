using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace TrainingLog.Tests;

public class WorkoutsControllerTests : IClassFixture<TrainingLogFactory>
{
    private readonly HttpClient _client;

    public WorkoutsControllerTests(TrainingLogFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<object> RunningSessionAsync(HttpClient client, string token, int? overrideTypeId = null)
    {
        var types = await client.WithToken(token).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        var running = types!.First(t => t.Name == "Running");
        var typeId = overrideTypeId ?? running.Id;
        var fieldId = running.Fields.First().Id;
        return new
        {
            WorkoutTypeId = typeId,
            LoggedAt = DateTime.UtcNow,
            Notes = (string?)null,
            Values = new[] { new { FieldDefinitionId = fieldId, Value = "5" } }
        };
    }

    private record WorkoutTypeResponse(int Id, string Name, List<FieldResponse> Fields);
    private record FieldResponse(int Id, string Name);

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
        var res = await _client.WithToken(token).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, token));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidWorkoutType_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, token, overrideTypeId: 99999));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetMine_ContainsNewSession()
    {
        var token = await Helpers.GetTokenAsync(_client, "bob", "bob");
        await _client.WithToken(token).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, token));

        var sessions = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutSessionResponse>>("/workouts");
        Assert.NotNull(sessions);
        Assert.NotEmpty(sessions);
    }

    [Fact]
    public async Task GetById_AdminCanSeeAnySession_ReturnsOk()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, aliceToken));
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(adminToken).GetAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetById_OtherUsersSession_Returns403()
    {
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var bobToken = await Helpers.GetTokenAsync(_client, "bob", "bob");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, aliceToken));
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(bobToken).GetAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnSession_ReturnsNoContent()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var created = await _client.WithToken(token).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, token));
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var del = await _client.WithToken(token).DeleteAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_OtherUsersSession_Returns403()
    {
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var bobToken = await Helpers.GetTokenAsync(_client, "bob", "bob");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, aliceToken));
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(bobToken).DeleteAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Delete_AdminCanDeleteAnySession_ReturnsNoContent()
    {
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", await RunningSessionAsync(_client, aliceToken));
        var session = await created.Content.ReadFromJsonAsync<WorkoutSessionResponse>();

        var res = await _client.WithToken(adminToken).DeleteAsync($"/workouts/{session!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).GetAsync("/workouts/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private record WorkoutSessionResponse(int Id, string Username);
}
