using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace TrainingLog.Tests;

public class AuthControllerTests : IClassFixture<TrainingLogFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(TrainingLogFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidAdmin_ReturnsOkWithToken()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = "admin", Password = "admin" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Equal("admin", body.User);
        Assert.Equal("admin", body.Role);
        Assert.NotEmpty(body.Token);
    }

    [Fact]
    public async Task Login_ValidUser_ReturnsOkWithToken()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = "alice", Password = "alice" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Equal("user", body.Role);
        Assert.NotEmpty(body.Token);
    }

    [Theory]
    [InlineData("admin", "wrong")]
    [InlineData("nobody", "pass")]
    public async Task Login_InvalidCredentials_Returns401(string username, string password)
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", "pass")]
    [InlineData("user", "")]
    public async Task Login_MissingCredentials_Returns400(string username, string password)
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithTooLongPassword_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = "admin", Password = new string('a', 73) });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private record LoginResponse(string User, string Role, string Token);
}
