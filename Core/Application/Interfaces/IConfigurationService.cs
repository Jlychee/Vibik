using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public record UserConfiguration(string Username, string Password);

public interface IConfigurationService
{
    Task<UserConfiguration> GetUserConfigurationAsync();
    Task<MapConfiguration> GetMapConfigurationAsync();
    Task SaveUserConfigurationAsync(UserConfiguration config);
}