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
}
