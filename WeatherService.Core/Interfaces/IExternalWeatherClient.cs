using WeatherService.Core.Entities;

namespace WeatherService.Core.Interfaces;

public interface IExternalWeatherClient
{
    /// <summary>
    /// Fetch current weather directly from the underlying third-party provider.
    /// </summary>
    Task<WeatherRecord?> FetchCurrentWeatherAsync(string location, CancellationToken cancellationToken = default);
}
