using Core.Domain;
using Task = System.Threading.Tasks.Task;

namespace Core.Interfaces;

public interface IAuthService
{
    Task SetTokensAsync(string accessToken, string? refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    void Logout();
    Task<LoginUserResponse?> TryRefreshTokensAsync(CancellationToken ct = default);
}