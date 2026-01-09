# PBI-08: Production Docker Configuration

## Status: Ready

## Description

Create the production-ready Docker configuration for deploying the API. This includes a multi-stage Dockerfile for optimized images and docker-compose for running the service with proper environment configuration.

## Acceptance Criteria

- [ ] `Dockerfile` uses multi-stage build:
  - [ ] Build stage: `mcr.microsoft.com/dotnet/sdk:8.0`
  - [ ] Runtime stage: `mcr.microsoft.com/dotnet/aspnet:8.0`
  - [ ] Final image contains only runtime dependencies
- [ ] `docker-compose.yml` defines:
  - [ ] `api` service building from Dockerfile
  - [ ] Port mapping 8080:8080
  - [ ] Environment variables for configuration (MAZE_DEFAULT_WIDTH, etc.)
  - [ ] ASPNETCORE_ENVIRONMENT set to Development
- [ ] Running `docker compose up --build` starts the API
- [ ] API responds to requests at `http://localhost:8080/api/mazes`
- [ ] Health check endpoint at `/health` returns 200 OK
- [ ] Image size is optimized (< 250MB)
- [ ] All environment variables documented in README or docker-compose.yml comments
- [ ] Smoke test: `docker compose up -d && curl http://localhost:8080/api/mazes` succeeds

## Dependencies

- PBI-07 (Move Through Maze)

## Technical Notes

### Multi-Stage Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/", "src/"]
COPY ["MazeOfHateoas.sln", "."]
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MazeOfHateoas.Api.dll"]
```

### docker-compose.yml
```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      # Runtime environment
      - ASPNETCORE_ENVIRONMENT=Development
      # Maze configuration
      - MAZE_DEFAULT_WIDTH=10
      - MAZE_DEFAULT_HEIGHT=10
      - MAZE_MAX_WIDTH=50
      - MAZE_MAX_HEIGHT=50
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### Health Check Endpoint
Add to `Program.cs`:
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

### Environment Variables
| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Production |
| `MAZE_DEFAULT_WIDTH` | Default maze width when not specified | 10 |
| `MAZE_DEFAULT_HEIGHT` | Default maze height when not specified | 10 |
| `MAZE_MAX_WIDTH` | Maximum allowed maze width | 50 |
| `MAZE_MAX_HEIGHT` | Maximum allowed maze height | 50 |

## Verification

```bash
# Build and run
docker compose up --build -d

# Check health
curl http://localhost:8080/health

# Smoke test
curl http://localhost:8080/api/mazes

# Check image size
docker images maze-of-hateoas-api --format "{{.Size}}"

# Cleanup
docker compose down
```
