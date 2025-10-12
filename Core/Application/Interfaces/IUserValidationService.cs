using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface IUserValidationService
{
    Task<User?> ValidateUserAsync(string username, string password);
    Task<bool> IsUserExistsAsync(string username);
}

