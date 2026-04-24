# Interview Presentation Guide: Weather Microservice

This guide is designed to help you confidently present your solution for **Challenge #1**. It focuses on the "Why" behind your technical decisions, demonstrating your senior-level engineering mindset.

---

## 🎤 1. The Elevator Pitch (The 30-Second Hook)
> "I built a resilient, production-ready Weather Microservice using **.NET 8** and **Clean Architecture**. My goal wasn't just to fetch data, but to design a system with **zero-config portability** using SQLite, **enterprise-grade resiliency** via Polly, and a fully automated **CI/CD pipeline** to Azure. It’s designed to be 'pulled and run' by a reviewer with no external setup required."

---

## 🗺️ 2. Presentation Outline (Slide-by-Slide)

### Slide 1: Problem & Approach
- **The Task**: Create a Weather Microservice with persistence, alerts, and resilience.
- **My Philosophy**: "Reviewer First" design. The code should work instantly on any machine.
- **Tech Stack**: .NET 8, EF Core (SQLite), Polly, xUnit, GitHub Actions, Azure.

### Slide 2: Architecture (Clean Layering)
- **Core**: Domain models & interfaces (No dependencies).
- **Infrastructure**: External API (OpenMeteo) and DB (SQLite) implementations.
- **API**: Controllers, Rate Limiting, and Swagger.
- **Benefit**: Decouples business logic from external tools (e.g., we can swap SQLite for SQL Server or OpenMeteo for AccuWeather without touching the Core logic).

### Slide 3: Resilience & Security
- **Polly**: Implemented standard resilience handlers (Retries, Circuit Breakers) for the external API.
- **Rate Limiting**: Global partitioned rate limiter (100 req/min) to prevent abuse.
- **Validation**: Strict input validation and cancellation token support across all async calls.

### Slide 4: Persistence & Key Features
- **SQLite Choice**: Explain that SQLite was a *strategic* choice for portability while still providing a full relational engine for SQL queries and EF Core.
- **CSV Export**: Demonstrated data transformation using `CsvHelper`.
- **Alerts**: Skeleton infrastructure for notification services.

### Slide 5: DevOps & Quality
- **Unit Tests**: Automated tests for services using Moq.
- **CI/CD**: GitHub Actions workflow for Build -> Test -> Deploy.
- **Cloud**: Live deployment on Azure App Service.

---

## 🛠️ 3. Technical Deep Dives (Be Ready to Explain These)

### 🧩 Why Clean Architecture?
**Answer:** "By separating the Core from Infrastructure, we ensure the business logic is testable and independent of external frameworks. If we need to upgrade the database or change the API provider, we only touch the Infrastructure layer."

### 💾 Why SQLite instead of SQL Server?
**Answer:** "For a microservice evaluation, I wanted to eliminate 'environmental friction'. SQLite provides a high-performance relational engine that resides in a single file. This ensures the reviewer doesn't need to install or configure a database server to see the full persistence and CSV export logic in action."

### 🛡️ What is 'Standard Resilience'?
**Answer:** "I used the `Microsoft.Extensions.Http.Resilience` package. It automatically adds a pipeline of Retries (for transient errors), Circuit Breakers (to stop hammering a failing service), and Timeouts. It ensures our microservice remains responsive even when the external Weather API is struggling."

---

## 🕹️ 4. Live Demo Script (The "Wow" Moment)

1.  **Open Swagger**: Show the interactive documentation at the [Live URL](https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger).
2.  **Fetch Weather**: Call `/api/v1/Weather/current/Singapore`.
    - *Point out*: "Notice how this isn't just a proxy; it's logging the data to our SQLite DB simultaneously."
3.  **Export CSV**: Execute `/api/v1/Weather/export/Singapore`.
    - *Point out*: "This pulls directly from our database cache and transforms it into a standard CSV format using `CsvHelper`."
4.  **Show CI/CD**: (Optional) Open the GitHub Actions tab.
    - *Point out*: "Every commit is automatically validated. This ensures that no broken code ever reaches the Azure production environment."

---

## ❓ 5. Anticipated Q&A

| Question | Recommended Answer |
| :--- | :--- |
| **How would you scale this for 1M users?** | "I'd swap SQLite for a distributed DB like Azure SQL or PostgreSQL, implement Redis caching for weather data, and scale the Azure App Service horizontally using a Load Balancer." |
| **Why OpenMeteo?** | "I chose it because it’s a high-quality, keyless API. This further reduces the setup burden for anyone running the code locally—no API keys required." |
| **How do you handle secrets?** | "Locally I use `appsettings.json` (or User Secrets). In production, I use GitHub Secrets and Azure App Service Configuration to ensure no sensitive data is committed to the repo." |

---

## ✅ Presentation Checklist
- [ ] Ensure the Azure site is awake (hit the URL once before the meeting).
- [ ] Have the code open in VS Code/Visual Studio.
- [ ] Be ready to show the `Program.cs` file (it’s the "brain" of the configuration).
- [ ] Practice the "Elevator Pitch" at least 3 times.

**Good luck! You've built a solid, professional-grade solution.**
