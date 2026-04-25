namespace WeatherService.Core.Entities;

// [ADDED - Feature: Request Logging]
// Core Layer: This is the data model (entity) that represents a single API request log entry.
// It lives in Core because it is a domain concept — a record of something that happened in the system.
// Infrastructure will persist it; Core only defines its shape.
public class ApiRequestLog
{
    public int Id { get; set; }

    // The location that was queried (e.g. "Singapore", "London")
    public string Location { get; set; } = string.Empty;

    // The API endpoint that was called (e.g. "GET /api/v1/weather/current/singapore")
    public string Endpoint { get; set; } = string.Empty;

    // UTC timestamp of when the request was received
    public DateTime RequestedAt { get; set; }
}
