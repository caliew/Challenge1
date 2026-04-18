# Weather Microservice

## 🌐 Live URLs (Azure Deployment)

**Live Raw API Test Endpoint:**
[https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/api/v1/weather/current/Singapore](https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/api/v1/weather/current/Singapore)

**Live Interactive Swagger Dashboard:**
[https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger](https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger)
*(Note: Swagger defaults to disabled in production, but was explicitly unlocked in `Program.cs` for this presentation)*


---

## 📝 Original Challenge Assignment
> **Challenge #1: Design and implement a Weather microservice**
> 
> Implement a Weather service API using C# / .NET Core. Weather data (e.g. temperature, humidity, air quality, etc) can be obtained from free 3rd party API such as https://data.gov.sg/ and OpenWeatherMap.
> 
> **Key Requirements**
> - Implement appropriate REST endpoints for a Weather service. Be creative about the use cases.
>   - Persist weather data in a database of your choice.
>   - Example use cases:
>     - Get current / forecast / historical weather by location.
>     - Export weather data by location into CSV.
>     - Subscribe to weather alerts.
> - Apply security best practices with resiliency in mind.
> - (🌟Bonus!) Configure OpenAPI with Swagger.
> - (🌟Bonus!) Setup CI/CD that builds and tests your solution.
> - (🌟Bonus!) Deploy your solution to a cloud service provider via CI/CD. Azure / AWS is preferred.

---

## 📋 Project Specification (Implementation Details)
This project fulfills the requirements for **Challenge #1: Design and implement a Weather microservice**. The goal was to build a resilient, production-ready service using C# / .NET Core.

### Core Requirements
- [x] **RESTful API**: Creative design of weather endpoints (Current, History, Forecast).
- [x] **External Integration**: Real-time data fetched from [OpenMeteo](https://open-meteo.com/).
- [x] **Data Persistence**: Integrated **SQLite + Entity Framework Core** (EF Core). SQLite was chosen as a strategic design decision to ensure the microservice is fully "portable" and zero-configuration—providing a relational storage engine for caching and CSV exports without requiring an external database server.
- [x] **Key Use Cases**:
    - Current, Forecast, and Historical data retrieval.
    - CSV Export for location-based weather data.
    - Weather Alerting infrastructure.
- [x] **Resilience & Security**: Implementation of rate-limiting and standard HTTP resilience policies.

### ⭐ Bonus Achievements
- [x] **OpenAPI Integration**: Full Swagger/OpenAPI documentation.
- [x] **Automated CI/CD**: End-to-end pipeline via GitHub Actions.
- [x] **Cloud Native**: Automated deployment to Azure App Service.

> [!TIP]
> **Technical Design Choice: Why SQLite?**
> Although the challenge only required general persistence, I opted for **SQLite + EF Core** to provide a self-contained, high-performance relational storage solution. This ensures the microservice is production-ready and fully functional (including background caching and CSV data export) the moment it is launched, with no external database dependencies or manual setup required for the reviewer.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Local Setup & Execution
1. **Restore Unit Test Dependencies**:
   Run this in the root directory:
   ```bash
   dotnet add WeatherService.Tests package Moq
   ```
2. **Build and Run**:
   ```bash
   dotnet run --project WeatherService.Api
   ```
3. **Access API**: Navigate to `https://localhost:<port>/swagger` (or see terminal output) to access the interactive Swagger UI.
4. **Try it out**: Visit `/api/Weather/current/Singapore`. The SQLite database (`weather.db`) is automatically initialized on startup inside the Data folder.

---

## 🛠️ Feature Deep Dive

- **Dynamic Weather Fetching**: Retrieves current, historical, and forecasted data with a global partitioned rate limit (100 req/min).
- **Service Resilience**: Utilizes `Microsoft.Extensions.Http.Resilience` to handle transient external failures with automated retries.
- **Data Export (CSV)**: A dedicated endpoint transforms cached database records into downloadable CSV format.
- **Alerting Framework**: A background-worker ready endpoint representing infrastructure for weather alert subscriptions.

---

## 🌐 CI/CD & Cloud Deployment

This repository uses a [GitHub Actions workflow](.github/workflows/ci-cd.yml) to perform formatting checks, tests, builds, and automated publishing.

### Azure Setup (For Reproduction)
To setup your own Azure free tier and connect it to this repo:
1. **Create Web App**: In Azure Portal, create a Web App (Publish: Code, Runtime: .NET 8, Plan: Free F1).
2. **Download Publish Profile**: From the App Service Overview page.
3. **Configure GitHub Secrets**:
   - Go to **Settings** > **Secrets and variables** > **Actions**.
   - Create `AZURE_WEBAPP_PUBLISH_PROFILE` and paste the XML contents.
4. **Update Workflow**: Ensure `AZURE_WEBAPP_NAME` in `.github/workflows/ci-cd.yml` matches your Azure resource name.
