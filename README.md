# Weather Microservice

A .NET 8 Web API demonstrating a weather microservice that integrates with an external provider (OpenMeteo), persists cached data locally with SQLite + EF Core, and incorporates typical microservice resiliency/rate-limiting best practices.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Running Locally

1. Add the missing `Moq` package for the test project.
   Run this in the root directory:
   ```bash
   dotnet add WeatherService.Tests package Moq
   ```
2. Build and run the project:
   ```bash
   dotnet run --project WeatherService.Api
   ```
3. A browser should open automatically (or you will see the localhost URL in the terminal, usually `https://localhost:5001`). Navigate to `https://localhost:<port>/swagger` to access the OpenAPI Swagger UI.
4. Try out the `/api/Weather/current/Singapore` endpoint! The application automatically generates the SQLite database (`weather.db` inside the Data folder) when it starts.

## API Features
- **Fetch weather:** Returns current, historical, and mocked forecast data. Includes global partitioned rate limit (100 req/min).
- **Service resilience:** Utilizes standard `.AddStandardResilienceHandler()` from Microsoft.Extensions.Http.Resilience to issue retries on transient external failures.
- **Export Data:** Endpoint retrieves cached entities from the database and returns it formatted as `.csv`.
- **Alerts base:** An initial Alerts endpoint representing background worker integration. 

## CI/CD Pipeline & Deployment (Azure App Service)

This solution contains a [GitHub Actions workflow](.github/workflows/ci-cd.yml) which performs formatting checks, tests, builds the DLLs, and publishes it to an Azure App Service using typical Azure deployments.

**To setup your own Azure free tier and connect it to this repo:**

1. **Create an Azure Account:** Head to [azure.microsoft.com](https://azure.microsoft.com) and sign up for a free tier account.
2. **Create a Web App:** In the Azure Portal, click **Create a resource** -> **Web App**.
   - **Publish:** Code.
   - **Runtime stack:** .NET 8 (LTS).
   - **Operating System:** Linux or Windows.
   - Pick the **Free (F1)** pricing plan.
3. **Download Publish Profile:** Once the App Service has been created, go to the Overview page and click **Download Publish Profile**.
4. **Link to GitHub Actions:**
   - Push this sourcecode repo to your GitHub account.
   - Go to your repository **Settings** > **Secrets and variables** > **Actions**.
   - Create a New repository secret.
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`.
   - Value: Paste the XML contents of the file you downloaded in Step 3 exactly as-is.
5. **Update CI/CD YAML:** Edit `.github/workflows/ci-cd.yml`. Make sure `AZURE_WEBAPP_NAME` in the environment variables exactly matches the name of your created web app.
6. The next time you commit to `main`, GitHub Actions will build, test, and instantly deploy the APIs directly to Azure!
