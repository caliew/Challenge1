namespace WeatherService.Core.Entities;

public class WeatherRecord
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public double HumidityPercentage { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    // [ADDED - Feature: Notification / Domain Logic]
    // Core Layer: This is a DOMAIN RULE living inside the entity.
    // The business rule is: "Singapore is a monitored location that triggers notifications."
    // By putting this here in Core, it is reusable anywhere without depending on Infrastructure.
    // WeatherAppService calls this method to decide whether to send a notification.
    public bool IsMonitoredLocation() =>
        Location.Equals("singapore", StringComparison.OrdinalIgnoreCase);
}
