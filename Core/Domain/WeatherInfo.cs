namespace Shared.Models;

public class WeatherInfo
{
    public double TemperatureCelsius { get; init; }

    public string Condition { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string IconCode { get; init; } = string.Empty;

    public DateTimeOffset RetrievedAt { get; init; } = DateTimeOffset.UtcNow;
}

