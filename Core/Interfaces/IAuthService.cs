namespace Core.Interfaces;

public interface IAuthService
{
    Task SetTokensAsync(string accessToken, string? refreshToken, string username);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    string? GetCurrentUser();
    void Logout();
    Task<bool> TryRefreshTokensAsync(CancellationToken ct = default);
}