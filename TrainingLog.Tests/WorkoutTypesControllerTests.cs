using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingLog.Data;
using Xunit;

namespace TrainingLog.Tests;

public class WorkoutTypesControllerTests : IClassFixture<TrainingLogFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TrainingLogFactory _factory;

    public WorkoutTypesControllerTests(TrainingLogFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.WorkoutSessions.RemoveRange(db.WorkoutSessions);
        var extra = await db.WorkoutTypes
            .Where(t => t.Name != "Running" && t.Name != "BJJ")
            .ToListAsync();
        db.WorkoutTypes.RemoveRange(extra);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    [Fact]
    public async Task GetById_WithUserToken_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var types = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        var first = types!.First();

        var res = await _client.WithToken(token).GetAsync($"/workout-types/{first.Id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).GetAsync("/workout-types/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOkWithNewName()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(token).PostAsJsonAsync("/workout-types", new
        {
            Name = "ToUpdate",
            Fields = Array.Empty<object>()
        });
        var body = await created.Content.ReadFromJsonAsync<WorkoutTypeResponse>();

        var res = await _client.WithToken(token).PutAsJsonAsync($"/workout-types/{body!.Id}", new
        {
            Name = "Updated",
            Fields = new[] { new { Name = "Reps", Type = 0, Unit = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var updated = await res.Content.ReadFromJsonAsync<WorkoutTypeResponse>();
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task Update_AsUser_Returns403()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var types = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        var first = types!.First();

        var res = await _client.WithToken(token).PutAsJsonAsync($"/workout-types/{first.Id}", new
        {
            Name = "Hacked",
            Fields = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PutAsJsonAsync("/workout-types/99999", new
        {
            Name = "Ghost",
            Fields = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var res = await _client.PostAsJsonAsync("/workout-types",
            new { Name = "Test", Fields = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workout-types",
            new { Name = "", Fields = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithNameTooLong_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workout-types",
            new { Name = new string('x', 101), Fields = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_AsUser_Returns403()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var types = await _client.WithToken(token).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        var res = await _client.WithToken(token).DeleteAsync($"/workout-types/{types!.First().Id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithNoFields_Returns201()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/workout-types",
            new { Name = "NoFields", Fields = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingSessions_Returns409()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");

        var created = await _client.WithToken(adminToken).PostAsJsonAsync("/workout-types", new
        {
            Name = "ToDeleteWithSessions",
            Fields = Array.Empty<object>()
        });
        var type = await created.Content.ReadFromJsonAsync<WorkoutTypeResponse>();

        await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", new
        {
            WorkoutTypeId = type!.Id,
            LoggedAt = DateTime.UtcNow,
            Notes = (string?)null,
            Values = Array.Empty<object>()
        });

        var del = await _client.WithToken(adminToken).DeleteAsync($"/workout-types/{type.Id}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Update_WithExistingSessions_Returns409()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var aliceToken = await Helpers.GetTokenAsync(_client, "alice", "alice");

        var created = await _client.WithToken(adminToken).PostAsJsonAsync("/workout-types", new
        {
            Name = "ToUpdateWithSessions",
            Fields = new[] { new { Name = "Reps", Type = 0, Unit = (string?)null } }
        });
        var type = await created.Content.ReadFromJsonAsync<WorkoutTypeResponse>();
        var fieldId = type!.Fields.First().Id;

        await _client.WithToken(aliceToken).PostAsJsonAsync("/workouts", new
        {
            WorkoutTypeId = type.Id,
            LoggedAt = DateTime.UtcNow,
            Notes = (string?)null,
            Values = new[] { new { FieldDefinitionId = fieldId, Value = "10" } }
        });

        var res = await _client.WithToken(adminToken).PutAsJsonAsync($"/workout-types/{type.Id}", new
        {
            Name = "ToUpdateWithSessions",
            Fields = new[] { new { Name = "Sets", Type = 0, Unit = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    private record FieldDefResponse(int Id, string Name, int Type, string? Unit);
    private record WorkoutTypeResponse(int Id, string Name, List<FieldDefResponse> Fields);
}
