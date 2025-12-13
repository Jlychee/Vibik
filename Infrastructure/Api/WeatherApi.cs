using System.Net.Http.Json;
using Core.Application;
using Domain.Models;

namespace Infrastructure.Api;

public class WeatherApi : IWeatherApi
{
    private readonly HttpClient httpClient;
    private readonly TimeSpan cacheDuration;
    private readonly SemaphoreSlim cacheLock = new(1, 1);

    private WeatherInfo? cached;
    private DateTimeOffset cacheExpiresAt;

    public WeatherApi(HttpClient httpClient, TimeSpan? cacheDuration = null)
    {
        this.httpClient = httpClient;
        this.cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct = default)
    {
        // кэш ещё живой — отдаем его
        if (cached != null && cacheExpiresAt > DateTimeOffset.UtcNow)
            return cached;

        await cacheLock.WaitAsync(ct);
        try
        {
            // пока ждали лок, кто-то мог уже обновить кэш
            if (cached != null && cacheExpiresAt > DateTimeOffset.UtcNow)
                return cached;

            // ❗ путь поправь под свой контроллер, если надо
            var weather = await httpClient.GetFromJsonAsync<WeatherInfo>(
                "api/Weather/current",
                cancellationToken: ct);

            if (weather is null)
                throw new InvalidOperationException("Weather endpoint returned empty response.");

            cached = weather;
            cacheExpiresAt = DateTimeOffset.UtcNow.Add(cacheDuration);
            return weather;
        }
        finally
        {
            cacheLock.Release();
        }
    }
}