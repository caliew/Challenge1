namespace WeatherService.Core.Entities;

/// <summary>
/// Represents a single day's weather forecast returned from an external provider.
/// Lives in Core so all layers can reference it without coupling to Infrastructure.
/// </summary>
public class ForecastDay
{
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public double MaxTemperatureCelsius { get; set; }
    public double MinTemperatureCelsius { get; set; }
    public string Condition { get; set; } = string.Empty;
}
