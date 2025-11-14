using Shared.Models;
using Task = Shared.Models.Task;

namespace Vibik.Services;

public interface IUserApi
{
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
}