# Requirements coverage

## Implemented

- Separate .NET API, Python AI service, and React frontend projects.
- Dockerfiles for all runtime services:
  - `src/CaptionGen.Api/Dockerfile`
  - `ai-service/Dockerfile`
  - `src/frontend/Dockerfile`
- Local Docker Compose stack with API, AI, frontend, PostgreSQL, and SonarQube.
- Clean Architecture and CQRS are already present in the backend:
  - Domain, Application, Infrastructure, API projects.
  - MediatR handlers, commands, queries, DTOs, and FluentValidation validators.
- Python AI service has modular FastAPI code and pytest coverage.
- Unit and integration test projects exist for .NET; integration tests use Testcontainers PostgreSQL.
- GitHub Actions CI/CD for build, tests, Sonar quality gate, Docker image push, and Azure deployment.
- Azure deployment with Container Apps, ACR, Azure Database for PostgreSQL, Blob Storage, and Log Analytics.
- Cloud media storage support through `MediaStorage:Provider=AzureBlob`.

## Manual setup still required

- Create GitHub secrets described in `deploy/azure/README.md`.
- Start Docker Desktop before running local Testcontainers integration tests or Docker Compose.
- Configure `OPENAI_API_KEY` for real AI caption generation.
- Configure Stripe keys and price IDs if payment flows must be fully live.
- Enable branch protection in GitHub so the CI/Sonar quality gate is required before PR merge.

## Notes

- The deployed frontend is the React/Vite app in `src/frontend`.
- The backend uses PostgreSQL via Npgsql, so the Azure deployment uses Azure Database for PostgreSQL instead of Azure SQL.
