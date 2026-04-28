using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;
using WeatherService.Infrastructure.Data;

namespace WeatherService.Infrastructure.Services;

public class WeatherAppService : IWeatherService
{
    private readonly WeatherDbContext _dbContext;
    private readonly IExternalWeatherClient _weatherClient;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WeatherAppService> _logger;

    // Infrastructure Layer: All dependencies are injected via DI (registered in Program.cs).
    // WeatherAppService only knows INTERFACES (Core contracts), never concrete implementations directly.
    // This is the Dependency Inversion Principle in action.
    public WeatherAppService(
        WeatherDbContext dbContext,
        IExternalWeatherClient weatherClient,
        INotificationService notificationService,
        ILogger<WeatherAppService> logger)
    {
        _dbContext = dbContext;
        _weatherClient = weatherClient;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<WeatherRecord> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request for current weather. Location: {Location}", location);

        // Log every inbound API request to the ApiRequestLogs table.
        var requestLog = new ApiRequestLog
        {
            Location    = location,
            Endpoint    = $"GET /api/v1/weather/current/{location}",
            RequestedAt = DateTime.UtcNow
        };
        _dbContext.ApiRequestLogs.Add(requestLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Request log persisted for {Location}", location);

        // 1. Fetch from external API (via OpenMeteoClient through Polly resilience pipeline)
        _logger.LogInformation("Calling external weather API for {Location}", location);
        var record = await _weatherClient.FetchCurrentWeatherAsync(location, cancellationToken);
        if (record == null)
        {
            _logger.LogWarning("External API returned no data for {Location}", location);
            throw new Exception("Could not fetch weather data.");
        }

        // 2. Persist weather result to DB (acts as a write-through cache for historical queries)
        _dbContext.WeatherRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Weather record persisted for {Location} at {Timestamp}", location, record.Timestamp);

        // Core Layer domain rule: IsMonitoredLocation() is defined on the WeatherRecord entity in Core.
        // Infrastructure calls it here but the decision logic lives in Core — not hardcoded in this service.
        if (record.IsMonitoredLocation())
        {
            _logger.LogInformation("Monitored location detected: {Location}. Dispatching notification.", location);
            await _notificationService.SendRequestNotificationAsync(
                toEmail:   "caliew888@gmail.com",
                location:  location,
                endpoint:  $"GET /api/v1/weather/current/{location}",
                cancellationToken: cancellationToken);
        }

        return record;
    }

    public async Task<IEnumerable<ForecastDay>> GetForecastAsync(string location, int days = 7, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received forecast request. Location: {Location}, Days: {Days}", location, days);

        // Forecasts are always live — no DB caching needed; just delegate to the external client.
        var forecast = await _weatherClient.FetchForecastAsync(location, days, cancellationToken);

        _logger.LogInformation("Forecast retrieved for {Location}. {Count} days returned.", location, forecast.Count());
        return forecast;
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
        _logger.LogInformation("Alert subscription request received. Email: {Email}, Location: {Location}", email, location);

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
            _logger.LogInformation("New alert subscription saved. Email: {Email}, Location: {Location}", email, location);
        }
        else
        {
            _logger.LogDebug("Duplicate subscription ignored. Email: {Email}, Location: {Location}", email, location);
        }
    }
}

