using Core.Domain;

namespace Core.Interfaces;

public interface IWeatherApi
{
    Task<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct = default);
}