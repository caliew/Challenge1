using WeatherService.Core.Entities;

namespace WeatherService.Core.Interfaces;

public interface IWeatherService
{
    /// <summary>
    /// Gets the current weather for a specified location. 
    /// Fetches from external API and caches to DB.
    /// </summary>
    Task<WeatherRecord> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the historically cached weather for a specified location and date.
    /// </summary>
    Task<IEnumerable<WeatherRecord>> GetHistoricalWeatherAsync(string location, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes an email base to weather alerts for a specific location.
    /// </summary>
    Task SubscribeToAlertsAsync(string email, string location, CancellationToken cancellationToken = default);
}
