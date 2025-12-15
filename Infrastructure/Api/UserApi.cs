using System.Net.Http.Json;
using Core.Domain;
using Core.Interfaces;

namespace Infrastructure.Api;

public class UserApi : IUserApi
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

    public async Task<LoginUserResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (useStub) return new LoginUserResponse("3f", "fd");

        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserLogin,
            new LoginRequest(username, password),
            ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LoginUserResponse>(cancellationToken: ct);
    }

    public async Task<User?> RegisterAsync(string username, string displayName, string password,
        CancellationToken ct = default)
    {
        if (useStub) return StubUser(username, displayName);

        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserRegister,
            new RegisterRequest(username, password, displayName),
            ct);
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

    private record LoginRequest(string Username, string Password);

    private record RegisterRequest(string Username, string DisplayName, string Password);
}