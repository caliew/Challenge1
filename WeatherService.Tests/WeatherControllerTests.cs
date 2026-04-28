using Microsoft.EntityFrameworkCore;
using Moq;
using WeatherService.Core.Entities;
using WeatherService.Core.Interfaces;
using WeatherService.Infrastructure.Data;
using WeatherService.Infrastructure.Services;
using WeatherService.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace WeatherService.Tests;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly WeatherController _controller;

    public WeatherControllerTests()
    {
        _mockWeatherService = new Mock<IWeatherService>();
        _controller = new WeatherController(_mockWeatherService.Object);
    }

    [Fact]
    public async Task GetCurrentWeather_ReturnsOk_WithWeatherRecord()
    {
        // Arrange
        var location = "Singapore";
        var expectedRecord = new WeatherRecord { Location = location, TemperatureCelsius = 30, Condition = "Clear sky" };
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRecord);

        // Act
        var result = await _controller.GetCurrentWeather(location, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var record = Assert.IsType<WeatherRecord>(okResult.Value);
        Assert.Equal(location, record.Location);
        Assert.Equal(30.0, record.TemperatureCelsius);
    }

    [Fact]
    public async Task GetForecast_ReturnsOk_WithForecastDays()
    {
        // Arrange
        var location = "Singapore";
        var expectedForecast = new List<ForecastDay>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Location = location, MaxTemperatureCelsius = 33, MinTemperatureCelsius = 26, Condition = "Clear sky" },
            new() { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), Location = location, MaxTemperatureCelsius = 31, MinTemperatureCelsius = 25, Condition = "Rain" }
        };
        _mockWeatherService
            .Setup(s => s.GetForecastAsync(location, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedForecast);

        // Act
        var result = await _controller.GetForecast(location, 7, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var days = Assert.IsAssignableFrom<IEnumerable<ForecastDay>>(okResult.Value);
        Assert.Equal(2, days.Count());
        Assert.Equal("Clear sky", days.First().Condition);
    }
}
