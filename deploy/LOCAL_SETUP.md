# Local setup

## Full Docker stack

Start Postgres, the .NET API, the Python AI service, the React frontend, and SonarQube:

```bash
OPENAI_API_KEY=sk-... docker compose -f deploy/docker-compose.yml up --build
```

Useful URLs:

- React frontend: http://localhost:8080
- .NET API: http://localhost:5080
- API Swagger: http://localhost:5080/docs
- API health: http://localhost:5080/health
- Python AI health: http://localhost:8001/health
- SonarQube: http://localhost:9000

The API host port can be changed with `API_PORT=...`. If you also change it, set `API_PUBLIC_BASE_URL` to the matching media URL.

If `OPENAI_API_KEY` is missing, the AI service still starts and exposes `/health`, but caption generation calls will fail until the key is configured.

## Run services manually

1. Start Postgres only:

```bash
docker compose -f deploy/docker-compose.yml up -d db
```

2. Run the Python AI service:

```bash
cd ai-service
uvicorn main:app --reload --port 8001
```

3. Run the .NET API:

```bash
cd src/CaptionGen.Api
dotnet run
```

4. Run the React frontend:

```bash
cd src/frontend
npm install
npm run dev
```

## Configuration notes

- `AiService:BaseUrl` points the .NET API to the Python service.
- `Database:AutoMigrate=true` applies EF migrations on API startup.
- `Seed:Enabled=true` creates the development demo user from `Seed:DemoEmail` and `Seed:DemoPassword`.
- `MediaStorage:Provider=Local` stores uploads under `/app/media` in Docker.
- `MediaStorage:Provider=AzureBlob` enables cloud blob storage through `MediaStorage:AzureBlob:*`.
