# Presentation Notes: Weather Microservice Challenge

## 1. The Architecture (Clean Layering)

We structured the solution into four specific projects instead of dumping everything into one file. This is an industry standard called "Clean Architecture", ensuring the code is modular and easy to maintain:

* **`WeatherService.Core`:** The absolute center of the app. It holds our bare-bones models (like what a `WeatherRecord` looks like) and interfaces. It has ZERO dependencies on other projects.
* **`WeatherService.Infrastructure`:** The "heavy lifter". This handles talking to the outside world—specifically talking to our SQLite Database and reaching out to the 3rd-party Weather API.
* **`WeatherService.Api`:** The "face" of the application. It contains the Controllers that listen to web traffic and Swagger UI.
* **`WeatherService.Tests`:** Our safety net containing automated xUnit tests to prevent future bugs.

## 2. The Core Tech Stack

* **.NET 8 Web API:** The latest long-term support framework for C#.
* **Open-Meteo API:** We cleverly chose an open-source weather provider so reviewers testing your code locally don't have to register for API keys.
* **SQLite + Entity Framework Core:** We chose SQLite because it creates a portable local `.db` file on the fly. Reviewers can just hit "run" and the database instantly creates itself without requiring them to install SQL Server!

## 3. Key Endpoints & Features

* **Current Weather:** We don't just fetch the current weather from Open-Meteo; we intercept that data and automatically log it into our local SQLite database to create a historical timeline.
* **CSV Export:** Instead of just sending JSON, we implemented `CsvHelper` to pull our database history and instantly download it to the user's browser as a formatted Excel-ready `.csv` file.
* **Alerts:** A simple subscription endpoint demonstrating how we can securely accept POST data.

## 4. Enterprise Grade Resiliency & Security

* **Polly (HTTP Resilience):** What happens if the 3rd-party weather API crashes or timeouts? We configured `AddStandardResilienceHandler()` which automatically adds retries and circuit-breakers so our app doesn't instantly crash if the external server hiccups.
* **Rate Limiting:** We added a Global Rate Limiter in `Program.cs` that restricts users to 100 requests per minute to protect your microservice from Denial of Service (DDoS) attacks.

## 5. Automated CI/CD

We wrote a GitHub Actions Workflow (`ci-cd.yml`). You can proudly present that every time code is pushed, a robot in the cloud checks out the code, builds it from scratch, and runs the unit tests. If the tests fail, it blocks the deployment, acting as an automated quality control gate!

## 6. How to Run Locally

To start the application locally, use the following command in your terminal:

```bash
dotnet run --project WeatherService.Api
```

## 7. Live URLs (Azure Deployment)

**Live Raw API Test Endpoint:**
[https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/api/weather/current/Singapore](https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/api/weather/current/Singapore)

**Live Interactive Swagger Dashboard:**
[https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger](https://weatherapp-gwdjgnh5hzbcdtcx.malaysiawest-01.azurewebsites.net/swagger)
*(Note: Swagger defaults to disabled in production, but was explicitly unlocked in `Program.cs` for this presentation)*

---

**⭐ Key Presentation Talking Point:**
> *"I designed this microservice with reviewer experience strictly in mind. Because I utilized SQLite and a keyless API framework, anyone can pull my repository and simply type `dotnet run`—it requires zero environment setup, zero database installations, and zero API keys to confidently evaluate."*
