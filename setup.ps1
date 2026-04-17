dotnet new sln -n WeatherService
dotnet new webapi --use-controllers -n WeatherService.Api -o WeatherService.Api
dotnet new classlib -n WeatherService.Core -o WeatherService.Core
dotnet new classlib -n WeatherService.Infrastructure -o WeatherService.Infrastructure
dotnet new xunit -n WeatherService.Tests -o WeatherService.Tests

dotnet sln add WeatherService.Api WeatherService.Core WeatherService.Infrastructure WeatherService.Tests

dotnet add WeatherService.Api reference WeatherService.Core WeatherService.Infrastructure
dotnet add WeatherService.Infrastructure reference WeatherService.Core
dotnet add WeatherService.Tests reference WeatherService.Api WeatherService.Core WeatherService.Infrastructure

dotnet add WeatherService.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add WeatherService.Infrastructure package Microsoft.Extensions.Http.Resilience
dotnet add WeatherService.Api package Microsoft.EntityFrameworkCore.Design
dotnet add WeatherService.Api package CsvHelper
