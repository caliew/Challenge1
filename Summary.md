# Modern .NET Web API & Clean Architecture Summary

The core of modern .NET Web API development isn't about learning complex new C# syntax. The code itself is standard C# programming. The true key to mastering it lies in understanding the **Design Patterns** and the **WebAPI Build Template**.

This architecture is optimized for scalability and is the industry standard for modern Client-Server microservices.

---

## 🏗️ 1. The Three-Layer Clean Architecture

The application is split into three distinct layers to enforce the separation of concerns:

### 🟢 Core Layer (The "What")
- **Pure C#:** Has absolutely zero external dependencies (no NuGet packages, no Entity Framework, no HTTP clients).
- **Responsibility:** Contains the pure **Business Logic** and **Domain Models**. 
- **Contents:** Entities (Data Models) and Interfaces (Service Contracts).
- *Example:* The rule `IsMonitoredLocation()` lives here because it is a pure business rule.

### 🟡 Infrastructure Layer (The "How")
- **The Orchestrator:** This is the application layer that makes the Core's rules happen in the real world.
- **Responsibility:** Handles all I/O operations—database access, calling external APIs, sending emails, and executing the workflow.
- **Contents:** `DbContext` (Entity Framework), HTTP Clients (e.g., calling Open-Meteo), and Service Implementations (e.g., `WeatherAppService`).
- *Example:* It asks Core, "Is this location monitored?" and if Core says yes, Infrastructure executes the SMTP email send.

### 🔵 API Layer (The "Entry Point")
- **The Front Door:** The top-most layer that receives HTTP requests from clients.
- **Responsibility:** Handles Routing, Security, Resiliency, and wiring up the application via Dependency Injection.
- **Contents:** Controllers (REST endpoints), `Program.cs` (Configuration), and `appsettings.json`.

---

## ⚙️ 2. Core Design Patterns

The architecture relies heavily on three primary patterns:

1. **MVC (Model-View-Controller) / MC:** 
   - Modern REST APIs drop the "V" (View) because they return raw JSON data instead of HTML pages.
   - **Controllers** define the REST API endpoints and route incoming HTTP requests to the appropriate services.
2. **Adapter Pattern:**
   - Used extensively in the Infrastructure layer to translate external systems into formats the Core layer understands (e.g., converting an Open-Meteo JSON response into a Core `WeatherRecord` entity).
3. **Dependency Injection (DI):**
   - The glue that holds the layers together. It allows the API layer to map a Core Interface to an Infrastructure Implementation at runtime.

---

## 🚀 3. The WebApplication Build Template (`Program.cs`)

The entire application is constructed in `Program.cs` using the `WebApplicationBuilder`. This is where all services are configured before the server starts:

1. **`AddControllers()`**: Activates the REST API endpoints.
2. **Swagger & OpenAPI (`AddEndpointsApiExplorer` / `AddSwaggerGen`)**: Auto-generates interactive API documentation for easy browser-based testing.
3. **Resiliency & Security**:
   - **`AddRateLimiter()`**: The *inbound shield* protecting the API from being overwhelmed by too many client requests.
   - **`AddHttpClient().AddStandardResilienceHandler()`**: The *outbound shield* (Polly) that uses Circuit Breakers and Retries to protect the API when third-party external services fail.
4. **Data Persistence (`AddDbContext`)**: Connects Entity Framework to the database (e.g., SQLite) for data storage.
5. **Dependency Injection Wiring (`AddScoped`)**: 
   - The most critical step. It links the pure interface definitions in the Core layer to the concrete execution classes in the Infrastructure layer.
   - *Example:* `builder.Services.AddScoped<IWeatherService, WeatherAppService>();`

---

### 💡 Final Takeaway
If you understand how `Program.cs` uses Dependency Injection to wire the isolated rules of the **Core** layer to the technical executors of the **Infrastructure** layer, you understand the heart of modern .NET enterprise architecture.
