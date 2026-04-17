namespace WeatherService.Core.Entities;

public class WeatherRecord
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public double HumidityPercentage { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
