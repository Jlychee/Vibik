using Domain.Models;

namespace Core.Application;

public interface IWeatherApi
{
    Task<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct = default);
}