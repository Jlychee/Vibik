using Shared.Models;

namespace Core.Application;

public interface IUserApi
{
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
    Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct = default);

    Task<LoginResponse?> RegisterAsync(string username, string displayName, string password, CancellationToken ct = default);
}