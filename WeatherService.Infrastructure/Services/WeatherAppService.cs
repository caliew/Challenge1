using Microsoft.EntityFrameworkCore;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;
using WeatherService.Infrastructure.Data;

namespace WeatherService.Infrastructure.Services;

public class WeatherAppService : IWeatherService
{
    private readonly WeatherDbContext _dbContext;
    private readonly IExternalWeatherClient _weatherClient;

    // [ADDED - Feature: Notification]
    // Infrastructure Layer: INotificationService is injected here via DI (registered in Program.cs).
    // WeatherAppService only knows the INTERFACE (Core contract), not EmailNotificationService directly.
    // This is the Dependency Inversion principle in action.
    private readonly INotificationService _notificationService;

    // [ADDED - Feature: Notification] — added INotificationService parameter
    public WeatherAppService(
        WeatherDbContext dbContext,
        IExternalWeatherClient weatherClient,
        INotificationService notificationService)  // [ADDED]
    {
        _dbContext = dbContext;
        _weatherClient = weatherClient;
        _notificationService = notificationService; // [ADDED]
    }

    public async Task<WeatherRecord> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        // [ADDED - Feature: Request Logging]
        // Infrastructure Layer: Log every inbound API request to the ApiRequestLogs table.
        // The ApiRequestLog entity is from Core; the persistence is done here in Infrastructure via EF Core.
        var requestLog = new ApiRequestLog
        {
            Location    = location,
            Endpoint    = $"GET /api/v1/weather/current/{location}",
            RequestedAt = DateTime.UtcNow
        };
        _dbContext.ApiRequestLogs.Add(requestLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1. Fetch from external API (via OpenMeteoClient through Polly resilience pipeline)
        var record = await _weatherClient.FetchCurrentWeatherAsync(location, cancellationToken);
        if (record == null)
            throw new Exception("Could not fetch weather data.");

        // 2. Persist weather result to DB (acts as a write-through cache for historical queries)
        _dbContext.WeatherRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // [ADDED - Feature: Notification / Domain Logic]
        // Core Layer rule: record.IsMonitoredLocation() is a DOMAIN RULE defined on the WeatherRecord entity in Core.
        // Infrastructure calls it here but the decision logic lives in Core — not hardcoded in this service.
        // If the location is a monitored location (e.g. Singapore), send a notification email.
        if (record.IsMonitoredLocation())
        {
            // The recipient email and the fact that Singapore is "monitored" is a business/config decision.
            // In a production system this would come from the AlertSubscriptions table or configuration.
            await _notificationService.SendRequestNotificationAsync(
                toEmail:   "caliew888@gmail.com",
                location:  location,
                endpoint:  $"GET /api/v1/weather/current/{location}",
                cancellationToken: cancellationToken);
        }

        return record;
    }

    public async Task<IEnumerable<WeatherRecord>> GetHistoricalWeatherAsync(string location, DateTime date, CancellationToken cancellationToken = default)
    {
        // Truncate time for date comparison
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await _dbContext.WeatherRecords
            .Where(w => w.Location.ToLower() == location.ToLower() && w.Timestamp >= startDate && w.Timestamp < endDate)
            .OrderByDescending(w => w.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task SubscribeToAlertsAsync(string email, string location, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.AlertSubscriptions.AnyAsync(s => s.Email == email && s.Location == location, cancellationToken);
        if (!exists)
        {
            _dbContext.AlertSubscriptions.Add(new AlertSubscription
            {
                Email = email,
                Location = location,
                SubscribedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

