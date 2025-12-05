using System.Net.Http.Json;
using System.Text.Json;
using Core.Application;
using Infrastructure.Services;
using Infrastructure.Utils;
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
            AccessToken = null,
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
                AccessToken = null,
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
        {
            await AppLogger.Warn(
                $"{{resp.IsSuccessStatusCode}}, {resp.StatusCode}, {resp.ReasonPhrase}, {resp.Content}");
            return null;
        }

        var service = new AuthService();
        var loginResponse = new LoginResponse
        {
            Username = username,
            DisplayName = displayName,
            AccessToken = service.GetAccessTokenAsync().ToString(),
            RefreshToken = service.GetRefreshTokenAsync().ToString()
        };


        return loginResponse;
    }

    private static async Task<LoginResponse?> ReadLoginResponseAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            if (resp.Content?.Headers.ContentLength == 0)
                return null;

            return await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        }
        catch (JsonException)
        {
            return null;
        }
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
