using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WeatherService.Core.Interfaces;
using WeatherService.Infrastructure.Clients;
using WeatherService.Infrastructure.Data;
using WeatherService.Infrastructure.Notifications; // [ADDED - Feature: Notification]
using WeatherService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Weather Microservice API", Version = "v1" });
});

// Configure Rate Limiting for security/resiliency 
// (limits clients to 100 requests per minute)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Configure Database (SQLite)
var dbFolder = Path.Combine(Environment.CurrentDirectory, "Data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "weather.db");

builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Configure Dependency Injection for Domain Services
builder.Services.AddScoped<IWeatherService, WeatherAppService>();

// [ADDED - Feature: Notification]
// Api Layer: Register INotificationService → EmailNotificationService in the DI container.
// WeatherAppService depends on INotificationService (Core interface).
// DI will inject EmailNotificationService (Infrastructure implementation) at runtime.
// SMTP credentials are read from appsettings.json (Smtp section).
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// Configure HttpClient for External API with Polly Resiliency
builder.Services.AddHttpClient<IExternalWeatherClient, OpenMeteoClient>()
    .AddStandardResilienceHandler(); // Adds standard retries, circuit breakers, and timeouts (requires Microsoft.Extensions.Http.Resilience)

var app = builder.Build();

// Configure the HTTP request pipeline.
// In a real enterprise app, keep this behind IsDevelopment(), 
// but for our presentation/challenge evaluation, we want Swagger permanently active!
app.UseSwagger();
app.UseSwaggerUI();

app.UseRateLimiter(); // Use Rate Limiting
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Automatic DB Migration (Create tables if they don't exist)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    db.Database.EnsureCreated(); // Creates the SQLite DB and tables on startup for ease of setup
}

app.Run();
