using Microsoft.EntityFrameworkCore;
using WeatherService.Core.Entities;

namespace WeatherService.Infrastructure.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
    {
    }

    public DbSet<WeatherRecord> WeatherRecords => Set<WeatherRecord>();
    public DbSet<AlertSubscription> AlertSubscriptions => Set<AlertSubscription>();

    // [ADDED - Feature: Request Logging]
    // Infrastructure Layer: Registers the ApiRequestLog entity as a DB table.
    // The entity (data model) is defined in Core; EF Core (Infrastructure) handles the actual persistence.
    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Additional constraints for weather records
        modelBuilder.Entity<WeatherRecord>()
            .HasIndex(w => new { w.Location, w.Timestamp });

        // [ADDED - Feature: Request Logging]
        // Index on RequestedAt for efficient time-based queries on the request log table
        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(r => r.RequestedAt);
    }
}
