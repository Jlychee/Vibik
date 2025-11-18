using Shared.Models;
using Task = Shared.Models.Task;

namespace Vibik.Services;

public interface IUserApi
{
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
    Task<User?> LoginAsync(string username, string password, CancellationToken ct = default);

    Task<User?> RegisterAsync(string username, string displayName, string password, CancellationToken ct = default);
}