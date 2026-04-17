namespace WeatherService.Core.Entities;

public class AlertSubscription
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime SubscribedAt { get; set; }
}
