namespace Infrastructure.Services;

public interface IAuthService
{
    Task SetTokensAsync(string accessToken, string? refreshToken, string username);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    string? GetCurrentUser();
    void Logout();
}