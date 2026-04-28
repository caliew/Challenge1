using WeatherService.Core.Entities;

namespace WeatherService.Core.Interfaces;

public interface IExternalWeatherClient
{
    /// <summary>
    /// Fetch current weather directly from the underlying third-party provider.
    /// </summary>
    Task<WeatherRecord?> FetchCurrentWeatherAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a multi-day daily forecast from the underlying third-party provider.
    /// </summary>
    Task<IEnumerable<ForecastDay>> FetchForecastAsync(string location, int days = 7, CancellationToken cancellationToken = default);
}
