using Core.Domain;

namespace Core.Interfaces;

public interface IUserApi
{
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
    Task<LoginUserResponse?> LoginAsync(string username, string password, CancellationToken ct = default);

    Task<bool> RegisterAsync(string username, string displayName, string password,
        CancellationToken ct = default);
}