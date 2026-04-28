namespace WeatherService.Core.Interfaces;

// [ADDED - Feature: Notification]
// Core Layer: This is the CONTRACT (interface) that defines what a notification service must do.
// Core does NOT know HOW the email is sent — that is Infrastructure's job.
// By defining the interface here in Core, the business logic (WeatherAppService) can call it
// without depending on any email library (SMTP, SendGrid, etc.).
public interface INotificationService
{
    /// <summary>
    /// Sends a weather request notification email to the specified recipient.
    /// Called when a monitored location (e.g. Singapore) is queried.
    /// </summary>
    Task SendRequestNotificationAsync(
        string toEmail,
        string location,
        string endpoint,
        CancellationToken cancellationToken = default);
}
