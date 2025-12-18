using Core.Domain;
using Task = System.Threading.Tasks.Task;

namespace Core.Interfaces;

public interface IAuthService
{
    Task SetTokensAsync(string inputAccessToken, string? inputRefreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    void Logout();
    Task<LoginUserResponse?> TryRefreshTokensAsync(CancellationToken ct = default);
}