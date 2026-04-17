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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Additional constraints
        modelBuilder.Entity<WeatherRecord>()
            .HasIndex(w => new { w.Location, w.Timestamp });
    }
}
