using System.Net.Http.Json;
using Core.Application;
using Shared.Models;

namespace Infrastructure.Api;

public class UserApi: IUserApi
{
    private readonly HttpClient httpClient;
    private readonly bool useStub;
    public UserApi(HttpClient httpClient, bool useStub = false)
    {
        this.httpClient = httpClient;
        this.useStub = useStub;
    }
    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        if (useStub) return StubUser(userId);
        return await httpClient.GetFromJsonAsync<User>(
            ApiRoutes.UserById(), 
            ct);
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (useStub) return new LoginResponse
        {
            Username = username,
            DisplayName = username,
            AccessToken = "stub-access-token",
            RefreshToken = null
        };

        var request = new
        {
            username,
            password
        };

        var resp = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserLogin,
            request,
            ct);

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
    }

    public async Task<LoginResponse?> RegisterAsync(string username, string password, string displayName, CancellationToken ct = default)
    {
        if (useStub)
        {
            return new LoginResponse
            {
                Username = username,
                DisplayName = displayName,
                AccessToken = "stub-access-token",
                RefreshToken = null
            };
        }

        var request = new
        {
            username,
            password,
            displayName
        };

        var resp = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserRegister,
            request,
            ct);

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
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
    
    private record LoginRequest(string Username, string Password);
    private record RegisterRequest(string Username, string DisplayName, string Password);
}
