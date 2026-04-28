using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;

namespace WeatherService.Infrastructure.Clients;

public class OpenMeteoClient : IExternalWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoClient> _logger;

    // Open-Meteo Geocoding API runs on a different base host, so we use absolute URLs for it.
    private const string GeocodingApiUrl = "https://geocoding-api.open-meteo.com/v1/search";

    public OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
        _logger = logger;
    }

    public async Task<WeatherRecord?> FetchCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        var (lat, lon) = await ResolveCoordinatesAsync(location, cancellationToken);
        var url = $"forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,weather_code";

        _logger.LogInformation("Calling Open-Meteo API. Location: {Location}, URL: {Url}", location, url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            _logger.LogInformation("Open-Meteo response received. StatusCode: {StatusCode}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(cancellationToken: cancellationToken);
            if (content?.Current == null)
            {
                _logger.LogWarning("Open-Meteo returned empty payload for Location: {Location}", location);
                return null;
            }

            _logger.LogDebug("Open-Meteo data parsed. Temperature: {Temp}C, Humidity: {Humidity}%, Code: {Code}",
                content.Current.Temperature_2m, content.Current.Relative_humidity_2m, content.Current.Weather_code);

            return new WeatherRecord
            {
                Location = location,
                TemperatureCelsius = content.Current.Temperature_2m,
                HumidityPercentage = content.Current.Relative_humidity_2m,
                Condition = MapWeatherCode(content.Current.Weather_code),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Open-Meteo failed for Location: {Location}", location);
            throw;
        }
    }

    public async Task<IEnumerable<ForecastDay>> FetchForecastAsync(string location, int days = 7, CancellationToken cancellationToken = default)
    {
        var (lat, lon) = await ResolveCoordinatesAsync(location, cancellationToken);
        var url = $"forecast?latitude={lat}&longitude={lon}&daily=temperature_2m_max,temperature_2m_min,weather_code&forecast_days={days}&timezone=auto";

        _logger.LogInformation("Calling Open-Meteo forecast API. Location: {Location}, Days: {Days}, URL: {Url}", location, days, url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            _logger.LogInformation("Open-Meteo forecast response received. StatusCode: {StatusCode}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<OpenMeteoForecastResponse>(cancellationToken: cancellationToken);
            if (content?.Daily == null)
            {
                _logger.LogWarning("Open-Meteo forecast returned empty daily payload for Location: {Location}", location);
                return Enumerable.Empty<ForecastDay>();
            }

            var daily = content.Daily;
            var count = daily.Time?.Length ?? 0;

            var result = new List<ForecastDay>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(new ForecastDay
                {
                    Date                  = DateOnly.Parse(daily.Time![i]),
                    Location              = location,
                    MaxTemperatureCelsius = daily.Temperature_2m_max![i],
                    MinTemperatureCelsius = daily.Temperature_2m_min![i],
                    Condition             = MapWeatherCode(daily.Weather_code![i])
                });
            }

            _logger.LogDebug("Open-Meteo forecast parsed. {Count} days returned for {Location}", result.Count, location);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Open-Meteo forecast failed for Location: {Location}", location);
            throw;
        }
    }

    /// <summary>
    /// Resolves a city name to (latitude, longitude) using the Open-Meteo Geocoding API.
    /// Replaces the old hardcoded GetCoordinates() switch — now supports any city worldwide.
    /// </summary>
    private async Task<(double lat, double lon)> ResolveCoordinatesAsync(string location, CancellationToken cancellationToken)
    {
        var geocodeUrl = $"{GeocodingApiUrl}?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";

        _logger.LogInformation("Resolving coordinates for city: {Location}", location);

        var geoResponse = await _httpClient.GetFromJsonAsync<GeocodingResponse>(geocodeUrl, cancellationToken);

        var result = geoResponse?.Results?.FirstOrDefault();
        if (result == null)
        {
            _logger.LogWarning("Geocoding API returned no results for city: {Location}", location);
            throw new ArgumentException($"City '{location}' could not be found. Please check the city name and try again.");
        }

        _logger.LogDebug("Resolved '{Location}' to lat={Lat}, lon={Lon} ({FullName})",
            location, result.Latitude, result.Longitude, result.Name);

        return (result.Latitude, result.Longitude);
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

    // ── DTOs ──────────────────────────────────────────────────────────────────

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

    private class OpenMeteoForecastResponse
    {
        [JsonPropertyName("daily")]
        public DailyBlock? Daily { get; set; }
    }

    private class DailyBlock
    {
        [JsonPropertyName("time")]
        public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m_max")]
        public double[]? Temperature_2m_max { get; set; }
        [JsonPropertyName("temperature_2m_min")]
        public double[]? Temperature_2m_min { get; set; }
        [JsonPropertyName("weather_code")]
        public int[]? Weather_code { get; set; }
    }

    // DTOs for Open-Meteo Geocoding API response
    private class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public GeocodingResult[]? Results { get; set; }
    }

    private class GeocodingResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
