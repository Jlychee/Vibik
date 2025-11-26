using Shared.Models;

namespace Vibik.Utils;

public class WeatherUtils
{
    public static string BuildWeatherInfoAboutFallout(WeatherInfo weather)
    {
        return weather.Condition.ToLowerInvariant() switch
        {
            "rain" or "drizzle" => "Возможны осадки",
            "snow" => "Возможен снег",
            "thunderstorm" => "Вероятна гроза",
            _ => "Осадков не ожидается"
        };
    }
    
    public static ImageSource? DefineWeatherImage(string normalized)
    {
        return normalized switch
        {
            "clear" => ImageSource.FromFile("sunny_weather.svg"),
            "clouds" => ImageSource.FromFile("cloudy_weather.svg"),
            "rain" or "drizzle" => ImageSource.FromFile("rain_weather.svg"),
            "snow" => ImageSource.FromFile("snow_weather.svg"),
            "thunderstorm" => ImageSource.FromFile("storm_weather.svg"),
            _ => null
        };
    }
}