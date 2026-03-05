using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TrainingLog.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
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
    [InlineData("", "")]
    public async Task Login_InvalidCredentials_Returns401(string username, string password)
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private record LoginResponse(string User, string Role, string Token);
}
