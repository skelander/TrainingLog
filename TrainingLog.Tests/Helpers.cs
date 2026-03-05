using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrainingLog.Tests;

public static class Helpers
{
    public static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var res = await client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        var data = await res.Content.ReadFromJsonAsync<LoginData>();
        return data!.Token;
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record LoginData(string User, string Role, string Token);
}
