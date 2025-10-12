using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface ISessionService
{
    Task<Session> InitializeSessionAsync();
    Task<User?> ValidateUserAsync(string username, string password);
    Task<MapConfiguration> InitializeMapAsync();
}

