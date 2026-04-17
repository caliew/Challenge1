using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;

namespace WeatherService.Infrastructure.Clients;

public class OpenMeteoClient : IExternalWeatherClient
{
    private readonly HttpClient _httpClient;

    public OpenMeteoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
    }

    public async Task<WeatherRecord?> FetchCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        // Simple mapping: Normally we would call a geocoding API to resolve the location to lat/lon.
        // For demonstration purposes without an external geocoding key, we'll hardcode a few common ones
        // or just default to Singapore's coordinates if unknown.
        var (lat, lon) = GetCoordinates(location);

        // Call open-meteo (requires lat/lon)
        var url = $"forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,weather_code";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(cancellationToken: cancellationToken);
        if (content?.Current == null)
        {
            return null;
        }

        return new WeatherRecord
        {
            Location = location,
            TemperatureCelsius = content.Current.Temperature_2m,
            HumidityPercentage = content.Current.Relative_humidity_2m,
            Condition = MapWeatherCode(content.Current.Weather_code),
            Timestamp = DateTime.UtcNow
        };
    }

    private static (double lat, double lon) GetCoordinates(string location)
    {
        // Mock geocoding. Real implementation would call something like openstreetmap geocoding API
        return location.ToLowerInvariant() switch
        {
            "singapore" => (1.3521, 103.8198),
            "london" => (51.5074, -0.1278),
            "new york" => (40.7128, -74.0060),
            "tokyo" => (35.6762, 139.6503),
            _ => (1.3521, 103.8198) // Default to Singapore
        };
    }

    private static string MapWeatherCode(int code)
    {
        // Map WMO Weather interpretation codes
        return code switch
        {
            0 => "Clear sky",
            1 or 2 or 3 => "Mainly clear, partly cloudy, and overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow fall",
            95 => "Thunderstorm",
            _ => "Unknown"
        };
    }

    private class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }

    private class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature_2m { get; set; }
        [JsonPropertyName("relative_humidity_2m")]
        public double Relative_humidity_2m { get; set; }
        [JsonPropertyName("weather_code")]
        public int Weather_code { get; set; }
    }
}
