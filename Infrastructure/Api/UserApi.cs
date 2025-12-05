using System.Net.Http.Json;
using Core.Application;
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
            ApiRoutes.User(), 
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
            var body = await resp.Content.ReadAsStringAsync(ct);
            await AppLogger.Warn(
                $"{{resp.IsSuccessStatusCode}}, {resp.StatusCode}, {resp.ReasonPhrase}, {body}");
            return null;
        }

        await AppLogger.Info("Регистрация прошла, пытаемся залогиниться тем же логином и паролем...");
        return await LoginAsync(username, password, ct);

    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (useStub)
            return null;

        var request = new { refreshToken };

        var resp = await httpClient.PostAsJsonAsync(ApiRoutes.UserRefresh, request, ct);

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
