# Technology Stack & Getting Started

## 🛠️ Technology Stack

- **Framework**: [.NET 8](https://dotnet.microsoft.com/)
- **API Style**: ASP.NET Core Minimal APIs
- **Design Patterns**: CQRS, Mediator, Repository, Pipeline Behavior
- **Mediator**: [MediatR](https://github.com/jbogard/MediatR)
- **Validation**: [FluentValidation](https://fluentvalidation.net/)
- **Security**: [Microsoft.AspNetCore.Authentication.JwtBearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer)
- **Telemetry, Metrics & Tracing**: [OpenTelemetry](https://opentelemetry.io/) (with ASP.NET Core, HttpClient, and Runtime instrumentation)
- **Interactive API Documentation**: Swagger/OpenAPI

---

## 🚀 Getting Started

### Prerequisites

To build and run this application, make sure you have:
- [Docker](https://www.docker.com/) and Docker Compose installed.
- (Optional) [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) if you want to run it natively on your machine.

### Running with Docker

You can easily run the application using the configured `Dockerfile` and `docker-compose.yml`.

1. **Build and start the container**:
   ```bash
   docker-compose up -d --build
   ```
2. **Verify it is running**:
   ```bash
   docker ps
   ```
   The API will start and expose its HTTP endpoint on port `5044`.

3. **Explore the Swagger UI**:
   Open your browser and navigate to:
   [http://localhost:5044/swagger/index.html](http://localhost:5044/swagger/index.html)

### Testing & Code Coverage

To execute the test suite:

- **Using standard .NET CLI**:
  ```bash
  dotnet test
  ```
- **Using a temporary Docker container (if SDK is not installed)**:
  ```bash
  docker run --rm -v "$(pwd):/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
  ```

#### Code Coverage Report
The project has **99.6% line coverage**. You can view the static coverage report inside the [coveragereport](file:///home/wt/Development/entrevistas/qt_bank/coveragereport) directory:
- Open [coveragereport/index.html](file:///home/wt/Development/entrevistas/qt_bank/coveragereport/index.html) directly in your browser.