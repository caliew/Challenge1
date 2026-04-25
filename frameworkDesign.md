# Challenge 1 — Framework Design

## Overview

This microservice is a **.NET 8 Web API** built around **Clean Architecture** principles. The solution is split into three separate C# projects, each with a strict dependency rule: outer layers depend on inner layers, never the reverse.

---

## Architecture: Clean Architecture (3-Layer)

```
┌─────────────────────────────────────────────┐
│           WeatherService.Api                │  ← Entry point, HTTP concerns
│  (Controllers, Program.cs, Rate Limiter,    │
│   Swagger, DI registration)                 │
└──────────────────┬──────────────────────────┘
                   │ depends on ↓
┌──────────────────▼──────────────────────────┐
│       WeatherService.Infrastructure         │  ← External concerns
│  (OpenMeteoClient, WeatherAppService,       │
│   WeatherDbContext / SQLite)                │
└──────────────────┬──────────────────────────┘
                   │ depends on ↓
┌──────────────────▼──────────────────────────┐
│          WeatherService.Core                │  ← Business heart (no deps)
│  (Entities: WeatherRecord, AlertSubscript.) │
│  (Interfaces: IWeatherService,              │
│               IExternalWeatherClient)       │
└─────────────────────────────────────────────┘
```

---

## Layer-by-Layer Breakdown

### 1. `WeatherService.Core` — The Domain Layer

> Zero external NuGet dependencies. Pure C# only.

This is the innermost layer and the most stable. It defines **what** the system does without caring **how** it is done.

| File | Role |
|---|---|
| `Entities/WeatherRecord.cs` | Data shape — Id, Location, Temp, Humidity, Condition, Timestamp |
| `Entities/AlertSubscription.cs` | Data shape — Email, Location, SubscribedAt |
| `Interfaces/IWeatherService.cs` | Contract for application business operations |
| `Interfaces/IExternalWeatherClient.cs` | Contract for any 3rd-party weather provider |

**Why this matters**: Core knows nothing about HTTP, databases, or Polly. You could swap the entire Infrastructure layer (e.g. switch from SQLite to PostgreSQL, or Open-Meteo to WeatherAPI.com) without changing a single line in Core.

---

### 2. `WeatherService.Infrastructure` — The Implementation Layer

> Implements Core's interfaces. Responsible for all I/O — network calls and database.

#### `OpenMeteoClient` (implements `IExternalWeatherClient`)

- Makes HTTP GET calls to `https://api.open-meteo.com/v1/`
- Resolves location names to lat/lon coordinates (hardcoded mock geocoding)
- Maps WMO weather interpretation codes → human-readable strings (e.g. `61` → `"Rain"`)
- Receives a plain `HttpClient` via constructor injection — **unaware** that Polly is wrapping it

#### `WeatherAppService` (implements `IWeatherService`)

Orchestrates the core business flow:

1. Call `IExternalWeatherClient.FetchCurrentWeatherAsync()` to get live data
2. Persist the result to SQLite via EF Core (acts as a write-through cache)
3. Return the `WeatherRecord` to the caller

Historical queries bypass the external API and query SQLite directly.
Alert subscription uses an `AnyAsync` check before insert to prevent duplicates.

#### `WeatherDbContext` (EF Core + SQLite)

- SQLite database, auto-created on startup via `db.Database.EnsureCreated()`
- Two `DbSet`s: `WeatherRecords` and `AlertSubscriptions`
- Compound index on `(Location, Timestamp)` for efficient historical range queries

**NuGet packages used:**
- `Microsoft.EntityFrameworkCore.Sqlite` v8.0.4
- `Microsoft.Extensions.Http.Resilience` v10.5.0

---

### 3. `WeatherService.Api` — The Presentation Layer

> ASP.NET Core Web API. The entry point that wires everything together via DI.

#### Controllers

| Controller | Method | Endpoint | Description |
|---|---|---|---|
| `WeatherController` | GET | `/api/v1/weather/current/{location}` | Fetch live weather, persist to DB |
| `WeatherController` | GET | `/api/v1/weather/forecast/{location}` | Mocked 3-day forecast |
| `WeatherController` | GET | `/api/v1/weather/historical/{location}?date=` | Query historical records from SQLite |
| `WeatherController` | GET | `/api/v1/weather/export/{location}` | Stream current-day records as CSV download |
| `AlertsController` | POST | `/api/v1/alerts/subscribe` | Subscribe an email to weather alerts for a location |

#### `Program.cs` — DI Composition Root

All services are registered here:

```csharp
// Rate Limiting — 100 requests/min per client IP (Fixed Window)
builder.Services.AddRateLimiter(...);

// SQLite database at ./Data/weather.db
builder.Services.AddDbContext<WeatherDbContext>(...);

// Business service
builder.Services.AddScoped<IWeatherService, WeatherAppService>();

// Typed HttpClient with Polly resilience pipeline attached
builder.Services.AddHttpClient<IExternalWeatherClient, OpenMeteoClient>()
    .AddStandardResilienceHandler();
```

**NuGet packages used:**
- `Swashbuckle.AspNetCore` v6.6.2 (Swagger/OpenAPI)
- `CsvHelper` v33.1.0 (CSV export)

---

## Resilience Design (Polly via `Microsoft.Extensions.Http.Resilience`)

The single `.AddStandardResilienceHandler()` call attaches a **pre-built Polly v8 pipeline** to the `HttpClient` used by `OpenMeteoClient`. Every outbound HTTP call flows through this pipeline:

```
Outbound Request
      │
  ┌───▼──────────────────────────┐
  │  1. Total Request Timeout    │  ← Hard stop for the entire operation (default: 30s)
  └───┬──────────────────────────┘
      │
  ┌───▼──────────────────────────┐
  │  2. Retry                    │  ← 3 retries, exponential backoff + jitter on 5xx/timeouts
  └───┬──────────────────────────┘
      │
  ┌───▼──────────────────────────┐
  │  3. Circuit Breaker          │  ← Opens after sustained failures; blocks requests during recovery
  └───┬──────────────────────────┘
      │
  ┌───▼──────────────────────────┐
  │  4. Attempt Timeout          │  ← Per-attempt cap (default: 10s) so slow calls don't consume retries
  └───┬──────────────────────────┘
      │
  ┌───▼──────────────────────────┐
  │  api.open-meteo.com          │  ← Actual network request
  └──────────────────────────────┘
```

### Circuit Breaker State Machine

```
        Failures > threshold
CLOSED ────────────────────▶ OPEN
  ▲                            │
  │   Probe succeeds           │  Wait (recovery window ~30s)
  │                            ▼
HALF-OPEN ◀─────────────── OPEN
  │   (1 test request let through)
  └── Fail → back to OPEN
```

- **CLOSED** — Normal operation, all requests pass through
- **OPEN** — Circuit tripped; requests fail immediately with `BrokenCircuitException` (no network hit)
- **HALF-OPEN** — One probe request is sent to test if the upstream service has recovered

---

## Rate Limiting Design

Built using ASP.NET Core's native `AddRateLimiter` (no third-party library needed):

- **Algorithm**: Fixed Window
- **Limit**: 100 requests per minute
- **Partition key**: Client IP address (`RemoteIpAddress`)
- **Rejection status**: `HTTP 429 Too Many Requests`

This protects the service from being overwhelmed by a single caller while remaining fair to all clients.

---

## Full Request Flow (End-to-End)

```
Client
  │  GET /api/v1/weather/current/singapore
  ▼
Rate Limiter          → reject with 429 if over quota
  ▼
WeatherController     → GetCurrentWeather(location, cancellationToken)
  ▼
IWeatherService       → WeatherAppService.GetCurrentWeatherAsync()
  ▼
IExternalWeatherClient → OpenMeteoClient.FetchCurrentWeatherAsync()
  ▼
HttpClient            → Polly pipeline (Timeout → Retry → Circuit Breaker → Attempt Timeout)
  ▼
api.open-meteo.com    → HTTP GET forecast?latitude=1.35&longitude=103.82&current=...
  ▼
Map response          → WeatherRecord entity
  ▼
EF Core               → INSERT into SQLite WeatherRecords table
  ▼
Return JSON           → { location, temperatureCelsius, humidity, condition, timestamp }
```

---

## Key Design Principles

| Principle | Where Applied |
|---|---|
| **Clean Architecture** | 3-project split: Core (domain) / Infrastructure (I/O) / Api (HTTP) |
| **Dependency Inversion** | Controllers depend on `IWeatherService`; AppService depends on `IExternalWeatherClient` — never on concrete classes |
| **Single Responsibility** | `OpenMeteoClient` only fetches; `WeatherAppService` only orchestrates; controllers only route |
| **Separation of Concerns** | Resilience is handled by the framework pipeline, not inside `OpenMeteoClient` |
| **Resilience** | Polly standard pipeline: circuit breaker, retry with backoff, dual timeouts |
| **Rate Limiting** | ASP.NET Core built-in fixed-window limiter, 100 req/min per IP |
| **Observability** | Swagger/OpenAPI always enabled for easy endpoint inspection and testing |
| **Data Persistence** | SQLite via EF Core — zero-config, file-based, suitable for embedded/demo scenarios |
| **Data Export** | CsvHelper for streaming CSV file downloads |

---

## Project Dependency Graph

```
WeatherService.Api
  ├── WeatherService.Core        (interfaces + entities)
  └── WeatherService.Infrastructure
        └── WeatherService.Core  (implements interfaces, uses entities)
```

`WeatherService.Core` has **no project references** — it is the stable foundation everything else builds on.
