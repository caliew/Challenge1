using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using WeatherService.Core.Interfaces;

namespace WeatherService.Infrastructure.Notifications;

// [ADDED - Feature: Notification]
// Infrastructure Layer: This is the CONCRETE IMPLEMENTATION of INotificationService (defined in Core).
// It is responsible for the HOW — sending emails via SMTP using System.Net.Mail (built into .NET).
// Core only knows about INotificationService. It never knows this class exists.
// This class is registered in Program.cs and injected into WeatherAppService via DI.
public class EmailNotificationService : INotificationService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    // [ADDED - Feature: Notification]
    // IConfiguration is injected so SMTP settings are read from appsettings.json.
    // This keeps credentials out of source code.
    public EmailNotificationService(IConfiguration configuration)
    {
        _smtpHost     = configuration["Smtp:Host"]     ?? "smtp.gmail.com";
        _smtpPort     = int.Parse(configuration["Smtp:Port"] ?? "587");
        _smtpUsername = configuration["Smtp:Username"] ?? string.Empty;
        _smtpPassword = configuration["Smtp:Password"] ?? string.Empty;
        _fromEmail    = configuration["Smtp:FromEmail"] ?? string.Empty;
        _fromName     = configuration["Smtp:FromName"]  ?? "Weather Service";
    }

    // [ADDED - Feature: Notification]
    // Implements INotificationService.SendRequestNotificationAsync (Core contract).
    // Sends an email alert to the specified recipient when a monitored location is queried.
    public async Task SendRequestNotificationAsync(
        string toEmail,
        string location,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = $"[Weather Service] Request received for {location}",
            Body = $"""
                    A weather API request has been made for a monitored location.

                    Location : {location}
                    Endpoint : {endpoint}
                    Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                    This is an automated notification from the Weather Microservice.
                    """,
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
