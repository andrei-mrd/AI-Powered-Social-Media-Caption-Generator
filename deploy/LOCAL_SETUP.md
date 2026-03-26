# Local setup (API + AI service + DB)

1) Start Postgres
```bash
docker compose -f deploy/docker-compose.yml up -d
```

2) Run the Python AI service (port 8001)
```bash
cd ai-service
uvicorn main:app --reload --port 8001
```

3) Run the .NET API (auto-migrates + seeds demo user in Development)
```bash
cd src/CaptionGen.Api
dotnet run
```

Health probes:
- API: `GET http://localhost:5000/health` (or your configured ASPNETCORE_URLS)
- AI service: `GET http://localhost:8001/health`

Configuration notes:
- `AiService:BaseUrl` / `TimeoutSeconds` / `HealthPath` are set per environment in `appsettings.*`.
- `Database:AutoMigrate` applies EF migrations on startup.
- `Seed:Enabled` (Development default) creates a demo account using `Seed:DemoEmail` / `Seed:DemoPassword`. Override in production if you do not want seeding.
