using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface IMapInitializationService
{
    Task<MapConfiguration> InitializeMapAsync();
    Task<MapConfiguration> LoadMapConfigurationAsync();
    Task SaveMapConfigurationAsync(MapConfiguration config);
}

