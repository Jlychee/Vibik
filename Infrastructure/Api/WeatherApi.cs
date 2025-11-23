using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Core.Application;
using Shared.Models;

namespace Infrastructure.Api;

public class WeatherService : IWeatherApi
{
    private static readonly Random rand = new Random();
    private const string DefaultLatitude = "56.8";
    private const string DefaultLongitude = "66.6";
    private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

    private readonly HttpClient httpClient;
    private readonly TimeSpan cacheDuration;
    private readonly bool useStub;
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private WeatherInfo? cached;
    private DateTimeOffset cacheExpiresAt;

    public WeatherService(HttpClient httpClient, TimeSpan? cacheDuration = null, string? apiKey = null)
    {
        this.httpClient = httpClient;
        this.cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
        var key = apiKey ?? Environment.GetEnvironmentVariable("WEATHER_API_KEY");
        useStub = string.IsNullOrWhiteSpace(key);
        if (!useStub)
            ApiKey = key!;
    }

    private string? ApiKey { get; }

    public async Task<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct = default)
    {
        if (cached != null && cacheExpiresAt > DateTimeOffset.UtcNow)
            return cached;

        await cacheLock.WaitAsync(ct);
        try
        {
            if (cached != null && cacheExpiresAt > DateTimeOffset.UtcNow)
                return cached;

            WeatherInfo info = useStub ? BuildStub() : await FetchFromApiAsync(ct);
            cached = info;
            cacheExpiresAt = DateTimeOffset.UtcNow.Add(cacheDuration);
            return info;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private async Task<WeatherInfo> FetchFromApiAsync(CancellationToken ct)
    {
        if (ApiKey is null)
            throw new InvalidOperationException("API key is not configured for weather service.");

        var url = $"{BaseUrl}?lat={DefaultLatitude}&lon={DefaultLongitude}&appid={ApiKey}&units=metric&lang=ru";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.AcceptLanguage.ParseAdd("ru");
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>(cancellationToken: ct);
        if (payload?.Weather is null || payload.Weather.Count == 0 || payload.Main is null)
            throw new InvalidOperationException("Weather response is missing required fields.");

        var w = payload.Weather[0];
        return new WeatherInfo
        {
            TemperatureCelsius = payload.Main.Temp,
            Condition = w.Main ?? "Unknown",
            Description = w.Description ?? string.Empty,
            IconCode = w.Icon ?? string.Empty,
            RetrievedAt = DateTimeOffset.UtcNow
        };
    }

    private static WeatherInfo BuildStub()
    {
        var weathers = new List<WeatherInfo>()
        {
            new WeatherInfo
            {
                TemperatureCelsius = 22,
                Condition = "Clouds",
                Description = "Облачно, данные с заглушки",
                RetrievedAt = DateTimeOffset.UtcNow
            },
            new WeatherInfo
            {
                TemperatureCelsius = 30,
                Condition = "Clear",
                Description = "Солнечно, данные с заглушки",
                RetrievedAt = DateTimeOffset.UtcNow
            },
            new WeatherInfo
            {
                TemperatureCelsius = 10,
                Condition = "rain",
                Description = "Дождик, данные с заглушки",
                RetrievedAt = DateTimeOffset.UtcNow
            },
            new WeatherInfo
            {
                TemperatureCelsius = 10,
                Condition = "snow",
                Description = "Oh, the weather outside is frightful\nBut the fire is so delightful\nSince we've no place to go\nLet it snow, let it snow, let it snow, данные с заглушки",
                RetrievedAt = DateTimeOffset.UtcNow
            },
            new WeatherInfo
            {
                TemperatureCelsius = 10,
                Condition = "thunderstorm",
                Description = "Гроза, данные с заглушки",
                RetrievedAt = DateTimeOffset.UtcNow
            }
        };
        var randIdx = rand.Next(weathers.Count);
        return weathers[randIdx];
    }

    private class OpenWeatherResponse
    {
        [JsonPropertyName("weather")]
        public List<WeatherDescription> Weather { get; set; } = [];

        [JsonPropertyName("main")]
        public TemperatureInfo? Main { get; set; }
    }

    private class WeatherDescription
    {
        [JsonPropertyName("main")]
        public string? Main { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
    }

    private class TemperatureInfo
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }
    }
}
