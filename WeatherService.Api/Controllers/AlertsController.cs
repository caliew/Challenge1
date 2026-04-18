using Microsoft.AspNetCore.Mvc;
using WeatherService.Core.Interfaces;

namespace WeatherService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public AlertsController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public class SubscribeRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest("Email and Location are required.");
        }

        await _weatherService.SubscribeToAlertsAsync(request.Email, request.Location, cancellationToken);
        
        return Ok(new { message = $"Successfully subscribed {request.Email} to alerts for {request.Location}." });
    }
}
