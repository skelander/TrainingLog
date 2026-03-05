using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TrainingLog.Tests;

public static class Helpers
{
    public static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var res = await client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        var data = await res.Content.ReadFromJsonAsync<LoginData>();
        return data!.Token;
    }

    public static TokenClient WithToken(this HttpClient client, string token) => new(client, token);

    private record LoginData(string User, string Role, string Token);
}

public class TokenClient(HttpClient client, string token)
{
    private HttpRequestMessage NewRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    public Task<HttpResponseMessage> GetAsync(string url) =>
        client.SendAsync(NewRequest(HttpMethod.Get, url));

    public Task<HttpResponseMessage> DeleteAsync(string url) =>
        client.SendAsync(NewRequest(HttpMethod.Delete, url));

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T value)
    {
        var req = NewRequest(HttpMethod.Post, url);
        req.Content = JsonContent.Create(value);
        return await client.SendAsync(req);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T value)
    {
        var req = NewRequest(HttpMethod.Put, url);
        req.Content = JsonContent.Create(value);
        return await client.SendAsync(req);
    }

    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        var res = await GetAsync(url);
        return await res.Content.ReadFromJsonAsync<T>();
    }
}
