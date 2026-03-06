using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingLog.Data;
using TrainingLog.Services;
using Xunit;

namespace TrainingLog.Tests;

public class UsersControllerTests : IClassFixture<TrainingLogFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TrainingLogFactory _factory;

    private static readonly HashSet<string> SeededUsernames = ["alice", "bob", "admin", "1"];

    public UsersControllerTests(TrainingLogFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.WorkoutSessions.RemoveRange(db.WorkoutSessions);
        var extra = await db.Users.Where(u => !SeededUsernames.Contains(u.Username)).ToListAsync();
        db.Users.RemoveRange(extra);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithUserToken_Returns403()
    {
        var token = await Helpers.GetTokenAsync(_client, "alice", "alice");
        var res = await _client.WithToken(token).GetAsync("/users");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        var users = JsonSerializer.Deserialize<List<UserResponse>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(users);
        Assert.Contains(users!, u => u.Username == "alice");
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreated()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "newuser",
            Password = "secret",
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var user = await res.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("newuser", user!.Username);
        Assert.Equal("user", user.Role);
    }

    [Fact]
    public async Task Create_DuplicateUsername_Returns409()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "alice",
            Password = "secret",
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidRole_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "roletest",
            Password = "secret",
            Role = "superuser"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyPassword_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "nopassword",
            Password = "",
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "toupdate",
            Password = "oldpass",
            Role = "user"
        });
        var user = await created.Content.ReadFromJsonAsync<UserResponse>();

        var res = await _client.WithToken(token).PutAsJsonAsync($"/users/{user!.Id}", new
        {
            Username = "updated",
            Password = (string?)null,
            Role = "admin"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("updated", updated!.Username);
        Assert.Equal("admin", updated.Role);
    }

    [Fact]
    public async Task Update_DuplicateUsername_Returns409()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "conflictuser",
            Password = "pass",
            Role = "user"
        });
        var user = await created.Content.ReadFromJsonAsync<UserResponse>();

        var res = await _client.WithToken(token).PutAsJsonAsync($"/users/{user!.Id}", new
        {
            Username = "alice",
            Password = (string?)null,
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = "todelete",
            Password = "pass",
            Role = "user"
        });
        var user = await created.Content.ReadFromJsonAsync<UserResponse>();

        var del = await _client.WithToken(token).DeleteAsync($"/users/{user!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).DeleteAsync("/users/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Delete_Self_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var users = await _client.WithToken(token).GetFromJsonAsync<List<UserResponse>>("/users");
        var adminUser = users!.First(u => u.Username == "admin");

        var res = await _client.WithToken(token).DeleteAsync($"/users/{adminUser.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_CascadesSessionsFromUser()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");

        // Create a new user
        var created = await _client.WithToken(adminToken).PostAsJsonAsync("/users", new
        {
            Username = "cascadetest",
            Password = "pass",
            Role = "user"
        });
        var newUser = await created.Content.ReadFromJsonAsync<UserResponse>();

        // Log a session as that user
        var userToken = await Helpers.GetTokenAsync(_client, "cascadetest", "pass");
        var types = await _client.WithToken(userToken).GetFromJsonAsync<List<WorkoutTypeResponse>>("/workout-types");
        var running = types!.First(t => t.Name == "Running");
        await _client.WithToken(userToken).PostAsJsonAsync("/workouts", new
        {
            WorkoutTypeId = running.Id,
            LoggedAt = DateTime.UtcNow,
            Notes = (string?)null,
            Values = Array.Empty<object>()
        });

        // Delete the user
        var del = await _client.WithToken(adminToken).DeleteAsync($"/users/{newUser!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Verify sessions were cascade-deleted (direct DB check)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.WorkoutSessions.AnyAsync(s => s.UserId == newUser.Id));
    }

    [Fact]
    public async Task GetById_AsAdmin_ReturnsOk()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var users = await _client.WithToken(token).GetFromJsonAsync<List<UserResponse>>("/users");
        var alice = users!.First(u => u.Username == "alice");
        var res = await _client.WithToken(token).GetAsync($"/users/{alice.Id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var user = await res.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("alice", user!.Username);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).GetAsync("/users/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PutAsJsonAsync("/users/99999", new
        {
            Username = "nobody",
            Password = (string?)null,
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithTooLongUsername_Returns400()
    {
        var token = await Helpers.GetTokenAsync(_client, "admin", "admin");
        var res = await _client.WithToken(token).PostAsJsonAsync("/users", new
        {
            Username = new string('a', 51),
            Password = "secret",
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_PasswordChange_NewPasswordWorks()
    {
        var adminToken = await Helpers.GetTokenAsync(_client, "admin", "admin");

        var created = await _client.WithToken(adminToken).PostAsJsonAsync("/users", new
        {
            Username = "pwchange",
            Password = "oldpassword",
            Role = "user"
        });
        var user = await created.Content.ReadFromJsonAsync<UserResponse>();

        var updated = await _client.WithToken(adminToken).PutAsJsonAsync($"/users/{user!.Id}", new
        {
            Username = "pwchange",
            Password = "newpassword",
            Role = "user"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = "pwchange", Password = "newpassword" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private record UserResponse(int Id, string Username, string Role);
    private record WorkoutTypeResponse(int Id, string Name, List<object> Fields);
}
