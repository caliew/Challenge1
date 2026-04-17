using Microsoft.EntityFrameworkCore;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;
using WeatherService.Infrastructure.Data;

namespace WeatherService.Infrastructure.Services;

public class WeatherAppService : IWeatherService
{
    private readonly WeatherDbContext _dbContext;
    private readonly IExternalWeatherClient _weatherClient;

    public WeatherAppService(WeatherDbContext dbContext, IExternalWeatherClient weatherClient)
    {
        _dbContext = dbContext;
        _weatherClient = weatherClient;
    }

    public async Task<WeatherRecord> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        // 1. Fetch from external
        var record = await _weatherClient.FetchCurrentWeatherAsync(location, cancellationToken);
        if (record == null)
            throw new Exception("Could not fetch weather data.");

        // 2. Persist to cache / db
        _dbContext.WeatherRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
