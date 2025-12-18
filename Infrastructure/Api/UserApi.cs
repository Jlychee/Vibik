using System.Net.Http.Json;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Utils;

namespace Infrastructure.Api;

public class UserApi(HttpClient httpClient) : IUserApi
{
    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<User>(
            ApiRoutes.User(),
            ct);
    }

    public async Task<LoginUserResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserLogin,
            new LoginRequest(username, password),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var parsedMessage = GetParsedErrorMessage(body);
            await AppLogger.Warn(
                $"LOGIN FAILED: {(int)response.StatusCode} {response.ReasonPhrase}; " +
                $"parsedMessage={parsedMessage}; raw={body}");
            throw new Exception(parsedMessage);
        }
        return await response.Content.ReadFromJsonAsync<LoginUserResponse>(cancellationToken: ct);
    }

    public async Task<bool> RegisterAsync(string username, string displayName, string password,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.UserRegister,
            new RegisterRequest(username, displayName, password),
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        await AppLogger.Info(
            $"RegisterAsync: status={(int)response.StatusCode} {response.ReasonPhrase}; body={body}");
        if (response.IsSuccessStatusCode) return true;
        var parsedMessage = GetParsedErrorMessage(body);
        await AppLogger.Warn(
            $"RegisterAsync FAILED: {(int)response.StatusCode} {response.ReasonPhrase}; " +
            $"parsedMessage={parsedMessage}; raw={body}");

        throw new Exception(parsedMessage);

    }

    private static string? GetParsedErrorMessage(string body)
    {
        ApiError? err = null;
        try
        {
            err = System.Text.Json.JsonSerializer.Deserialize<ApiError>(
                body, new System.Text.Json.JsonSerializerOptions {PropertyNameCaseInsensitive = true});
        }
        catch
        {
            //ignored
        }

        var parsedMessage = err?.Message ?? err?.Error ?? err?.Detail;
        return parsedMessage;
    }

    private record LoginRequest(string Username, string Password);

    private record RegisterRequest(string Username, string DisplayName, string Password);
    public sealed record ApiError(string? Message, string? Error, string? Detail);

}