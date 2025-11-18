using System.Net.Http.Json;
using Shared.Models;

namespace Vibik.Services;

public class UserApi(HttpClient httpClient, bool useStub = false): IUserApi
{
    public static UserApi Create(string baseUrl, bool useStub = false, HttpMessageHandler? handler = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        return new UserApi(client, useStub);
    }

    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        if (useStub) return StubUser(userId);
        return await httpClient.GetFromJsonAsync<User>($"api/users/{Uri.EscapeDataString(userId)}", ct);
    }

    public async Task<User?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (useStub) return StubUser(username);

        var response = await httpClient.PostAsJsonAsync("api/users/login", new LoginRequest(username, password), ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<User>(cancellationToken: ct);
    }

    public async Task<User?> RegisterAsync(string username, string displayName, string password, CancellationToken ct = default)
    {
        if (useStub) return StubUser(username, displayName);

        var response = await httpClient.PostAsJsonAsync("api/users/register", new RegisterRequest(username, displayName, password), ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<User>(cancellationToken: ct);
    }

    private static User StubUser(string username, string? displayName = null)
    {
        return new User
        {
            Username = username,
            DisplayName = displayName ?? "Тестовый пользователь",
            Level = 5,
            Experience = 125
        };
    }
    
    private sealed record LoginRequest(string Username, string Password);
    private sealed record RegisterRequest(string Username, string DisplayName, string Password);
}
