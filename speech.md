# Challenge #1 — Presentation Speech

> ⏱️ Estimated delivery: ~2 minutes

---

## 🎤 Speech Script

"For Challenge #1, I implemented a weather microservice using Clean Architecture with a layered project structure."

"The baseline is **WeatherService.Core** – a project with no external dependencies. It contains the data models and interface contracts that define core business rules."

"On top of that is **WeatherService.Infrastructure** – the infrastructure layer that implements the Core interfaces and manages external integrations. I grouped this into three folders:
- **Client** – calls the Open-Meteo REST API to fetch live weather data
- **Data** – handles the DbContext with Entity Framework Core using SQLite for portable, zero-config persistence
- **Service** – orchestrates the Client, Data, and Core modules together"

"The main entry point is **WeatherService.API** – built with ASP.NET Core. It follows the **MVC design pattern**: the **Controller** (`WeatherController`) handles incoming HTTP requests and returns responses, but contains zero business logic — it delegates immediately to the **Service** (`IWeatherService`), which acts as the Model layer orchestrating data retrieval and persistence. The JSON response returned via `IActionResult` serves as the View. Swagger/OpenAPI is enabled for interactive documentation."

"A key design pattern used here is **Dependency Injection** — all wiring is done in `Program.cs` through the service container:
- `AddScoped<IWeatherService, WeatherAppService>()` — injects the Infrastructure implementation whenever a controller requests the Core interface, enforcing the **Dependency Inversion Principle**
- `AddHttpClient<IExternalWeatherClient, OpenMeteoClient>().AddStandardResilienceHandler()` — registers the HTTP client with automatic retries and circuit breakers via `Microsoft.Extensions.Http.Resilience`
- `AddRateLimiter()` — wires a global fixed-window rate limiter, capping each IP at 100 requests per minute
- `AddDbContext<WeatherDbContext>()` — configures Entity Framework Core with the SQLite connection string

This means the API layer never calls `new` on any concrete class — all dependencies are resolved and injected at runtime, keeping all layers loosely coupled and independently testable."

"For quality assurance, I wrote unit tests using **xUnit** and **Moq** for mocking. The tests cover the core orchestration logic in the WeatherService class — verifying correct data aggregation from the client and repository layers under different scenarios, such as successful retrieval and null response handling."

"Beyond the core requirements, I also implemented:
- **CSV export** by location using `CsvHelper`
- **Weather alert subscriptions** using a background service
- **CI/CD pipeline** with GitHub Actions that builds, tests, and deploys to Azure App Service"

"Regarding production observability: in the **WeatherService** class within the Infrastructure layer, I would inject `ILogger<WeatherService>` and add structured log entries before external API calls, on successful database writes, and within each exception handler — feeding into Application Insights for monitoring and root cause analysis."

"The complete code is on GitHub — I'm happy to walk through any part in detail."

---

## 📌 Key Talking Points (If Asked)

| Topic | What to Say |
|---|---|
| **Why Clean Architecture?** | Business logic is isolated and testable. Swap SQLite → SQL Server or Open-Meteo → AccuWeather without touching Core. |
| **Why SQLite?** | Zero-config portability. Reviewer just runs `dotnet run` — no DB installation, no API keys needed. |
| **What is Standard Resilience?** | `Microsoft.Extensions.Http.Resilience` adds a pipeline of retries, circuit breakers, and timeouts automatically. |
| **How to scale to 1M users?** | Swap SQLite for Azure SQL/PostgreSQL, add Redis caching, scale App Service horizontally with a load balancer. |
| **How are secrets managed?** | Locally via `appsettings.json`. In production, GitHub Secrets + Azure App Service Configuration. |
| **What is Dependency Injection here?** | `AddScoped<IWeatherService, WeatherAppService>()` wires the Core interface to its Infrastructure implementation. The controller receives it via constructor injection — never a `new` keyword in sight. |
| **Why is `weather.db` in the API project, not Infrastructure?** | Infrastructure compiles into a DLL — it owns `WeatherDbContext`, the code that defines how to use the database. The `.db` file is a runtime artefact created by the running process. Runtime files follow the process; code follows the layer. |

---

## 🔗 Live URLs

| Resource | URL |
|---|---|
| **API Endpoint** | https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/api/weather/current/Singapore |
| **Swagger UI** | https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger |
| **GitHub Repo** | https://github.com/caliew/Challenge1 |

---

## ✅ Pre-Presentation Checklist

- [ ] Hit the Azure URL once beforehand to wake the app (cold start)
- [ ] Have VS Code open on `Program.cs` — it shows the full DI and middleware configuration
- [ ] Have Swagger UI ready in the browser
- [ ] Practice the speech at least twice to hit the 2-minute mark
