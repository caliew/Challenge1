# Feature Implementation: Request Logging & Notifications

This document outlines the newly added features—**API Request Logging** and **Email Notifications for Monitored Locations**—and how they perfectly demonstrate Clean Architecture principles in action.

## 🌟 What Was Done

1. **API Request Logging**: Every time an inbound request hits the weather API, it is recorded in a local SQLite database table (`ApiRequestLogs`).
2. **Targeted Email Notifications**: If a user queries the weather for a "monitored location" (specifically "Singapore"), an email notification is automatically dispatched to the administrator (`caliew888@gmail.com`).

---

## 🚀 How to Fire Up and Test

### 1. Prerequisites (Email Configuration)
To allow the application to send emails via Gmail, an **App Password** is required.
1. Go to your Google Account: [App Passwords](https://myaccount.google.com/apppasswords).
2. Generate a 16-character App Password for "Mail".
3. Open `WeatherService.Api/appsettings.json`.
4. Replace `"cllu vohk qpgj zesh"` (or placeholder) with your generated App Password.

### 2. Reset the Database
Because we added a new table (`ApiRequestLogs`), the existing SQLite database file needs to be deleted so Entity Framework can recreate it with the new schema.
- **Action**: Delete the file at `Data/weather.db`.

### 3. Run the Application
Open your terminal in the `Challenge1` folder and start the service:
```powershell
dotnet run --project WeatherService.Api
```

### 4. Test via Swagger
1. Open a browser and navigate to the Swagger UI: `https://localhost:<port>/swagger`
2. Test a **monitored location**:
   - Execute `GET /api/v1/weather/current/singapore`
   - **Result**: You will see a successful JSON response, a log entry will be saved to the database, and you will receive an email in your inbox.
3. Test a **standard location**:
   - Execute `GET /api/v1/weather/current/london`
   - **Result**: You will get a successful response and a database log, but **no email** will be sent.

---

## 🏗️ Clean Architecture Flow in Action

The implementation of these features perfectly illustrates how concerns are separated across the three layers of Clean Architecture.

### 1. The Entry Point: API Layer (`WeatherService.Api`)
*The API layer handles HTTP traffic and wires up dependencies.*
- **Action**: The user calls `GET /api/v1/weather/current/singapore`.
- **Flow**: The `WeatherController` receives the HTTP request and delegates the work to the `IWeatherService` interface. The controller knows nothing about emails or databases.
- **Wiring (`Program.cs`)**: The DI container injects `WeatherAppService` (for `IWeatherService`) and `EmailNotificationService` (for `INotificationService`). It also pulls the SMTP credentials from `appsettings.json`.

### 2. The Domain: Core Layer (`WeatherService.Core`)
*The Core layer defines "What" the system is and the business rules. It has ZERO external dependencies.*
- **Data Shapes**: Defines `ApiRequestLog` (the structure of a log) and `INotificationService` (the contract defining that notifications *can* be sent).
- **Business Logic**: This is where the magic happens. The rule defining what makes a location "monitored" lives inside the `WeatherRecord` entity itself:
  ```csharp
  public bool IsMonitoredLocation() =>
      Location.Equals("singapore", StringComparison.OrdinalIgnoreCase);
  ```
  *Why here?* If the business decides to monitor "Tokyo" tomorrow, we only change this pure C# rule. We don't touch databases or email clients.

### 3. The Execution: Infrastructure Layer (`WeatherService.Infrastructure`)
*The Infrastructure layer handles the "How". It does the dirty work of talking to external systems (DBs, APIs, SMTP).*
- **Database (`WeatherDbContext`)**: Takes the `ApiRequestLog` Core entity and physically saves it to the SQLite `weather.db` file.
- **Email (`EmailNotificationService`)**: Implements the Core `INotificationService` contract using Microsoft's `SmtpClient` to actually send the email over the network.
- **The Orchestrator (`WeatherAppService`)**: This is the conductor. It executes the flow step-by-step:
  1. Creates an `ApiRequestLog` object and saves it via `_dbContext`.
  2. Fetches the weather via the `OpenMeteoClient`.
  3. Saves the fetched weather to `_dbContext`.
  4. **The Handshake**: It asks the Core entity to evaluate the business rule: `if (record.IsMonitoredLocation())`.
  5. If true, it commands the `EmailNotificationService` to send the alert.

### 🎯 Summary
By organizing the code this way, the **Business Rule** (Core) is decoupled from the **Email Delivery Mechanism** (Infrastructure) and the **HTTP Request** (Api). You could easily swap the Email sender for an SMS sender, or the SQLite database for SQL Server, without ever touching the core business logic.
