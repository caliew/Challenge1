using Microsoft.AspNetCore.Mvc;
using WeatherService.Core.Interfaces;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace WeatherService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("current/{location}")]
    public async Task<IActionResult> GetCurrentWeather(string location, CancellationToken cancellationToken)
    {
        var record = await _weatherService.GetCurrentWeatherAsync(location, cancellationToken);
        return Ok(record);
    }

    [HttpGet("forecast/{location}")]
    public IActionResult GetForecast(string location)
    {
        // For demonstration, mock a 3-day forecast
        var forecast = Enumerable.Range(1, 3).Select(index => new
        {
            Date = DateTime.UtcNow.AddDays(index),
            Location = location,
            TemperatureCelsius = Random.Shared.Next(-20, 55),
            Summary = "Mocked Forecast"
        });

        return Ok(forecast);
    }

    [HttpGet("historical/{location}")]
    public async Task<IActionResult> GetHistorical(string location, [FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var records = await _weatherService.GetHistoricalWeatherAsync(location, date, cancellationToken);
        return Ok(records);
    }

    [HttpGet("export/{location}")]
    public async Task<IActionResult> ExportWeatherCsv(string location, CancellationToken cancellationToken)
    {
        // For demonstration, exporting the historical data of the current day. 
        // In a real app, date range would be provided.
        var records = await _weatherService.GetHistoricalWeatherAsync(location, DateTime.UtcNow, cancellationToken);
        
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        
        await csv.WriteRecordsAsync(records, cancellationToken);
        await writer.FlushAsync();
        
        memoryStream.Position = 0;
        
        return File(memoryStream, "text/csv", $"weather_{location}.csv");
    }
}
