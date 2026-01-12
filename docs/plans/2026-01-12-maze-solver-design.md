# Maze Solver Console Application - Design Document

## Overview

A console application that continuously generates and solves mazes via the HATEOAS API. It demonstrates proper HATEOAS client behavior by navigating exclusively through hypermedia links.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Navigation approach | Intelligent HATEOAS follower | Tracks visited cells, avoids backtracking. Demonstrates HATEOAS properly while being efficient. |
| Project location | Same solution (`src/MazeOfHateoas.Solver/`) | Shares patterns/infrastructure, keeps monorepo together |
| Logging | Configurable verbosity | Debug for moves, Information for summaries. User controls via log level. |
| API connection | External URL or Docker Compose | Flexibility for dev (compose) and production (external API) |
| Loop behavior | Configurable delay with metrics | Readable logs, periodic stats every N mazes |

## Project Structure

```
src/
├── MazeOfHateoas.Api/
├── MazeOfHateoas.Application/
├── MazeOfHateoas.Domain/
├── MazeOfHateoas.Infrastructure/
└── MazeOfHateoas.Solver/
    ├── Program.cs
    ├── Configuration/
    │   └── SolverSettings.cs
    ├── Services/
    │   ├── IMazeApiClient.cs
    │   ├── MazeApiClient.cs
    │   ├── ISolver.cs
    │   └── HateoasSolver.cs
    └── Models/
        ├── MazeResponse.cs
        ├── SessionResponse.cs
        └── Link.cs

tests/
└── MazeOfHateoas.Solver.UnitTests/
```

**Dependencies:**
- `Microsoft.Extensions.Hosting` - hosted service pattern for graceful shutdown
- `Microsoft.Extensions.Http` - `IHttpClientFactory` for HTTP client management
- `Microsoft.Extensions.Http.Resilience` - Polly retry policies
- `Serilog.Extensions.Hosting` - structured JSON logging

No references to other `MazeOfHateoas.*` projects - pure HTTP client treating API as external service.

## Configuration

All settings from environment variables (12-factor):

```csharp
public class SolverSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:8080";
    public int MazeWidth { get; set; } = 10;
    public int MazeHeight { get; set; } = 10;
    public int DelayBetweenMazesMs { get; set; } = 2000;
    public int DelayBetweenMovesMs { get; set; } = 0;
    public int StatsIntervalMazes { get; set; } = 10;
}
```

| Variable | Description | Default |
|----------|-------------|---------|
| `SOLVER_API_BASE_URL` | API endpoint | `http://localhost:8080` |
| `SOLVER_MAZE_WIDTH` | Width for generated mazes | 10 |
| `SOLVER_MAZE_HEIGHT` | Height for generated mazes | 10 |
| `SOLVER_DELAY_BETWEEN_MAZES_MS` | Pause after solving | 2000 |
| `SOLVER_DELAY_BETWEEN_MOVES_MS` | Pause between moves (0 = none) | 0 |
| `SOLVER_STATS_INTERVAL_MAZES` | Log stats every N mazes | 10 |
| `Serilog__MinimumLevel__Default` | Log verbosity | `Information` |

## Solving Algorithm

Depth-first search with backtracking, tracking visited cells:

```
1. Start session via HATEOAS "start" link
2. Initialize visited set with starting position
3. While session state is "InProgress":
   a. Get available moves from response "_links" (north, south, east, west)
   b. Filter to unvisited positions
   c. If unvisited moves exist:
      - Push current position to backtrack stack
      - Choose first unvisited direction
      - Move via HATEOAS link
      - Mark new position as visited
   d. If no unvisited moves (dead end):
      - Pop from backtrack stack
      - Move back toward that position
4. Session complete when state becomes "Completed"
```

**Key points:**
- Pure HATEOAS navigation - only uses links present in responses
- Position tracking via `HashSet<(int X, int Y)>`
- Backtrack stack remembers path to retrace at dead ends
- Guaranteed completion - DFS always finds exit if one exists

## Logging Strategy

Structured JSON logging with Serilog:

| Level | What's Logged |
|-------|---------------|
| `Debug` | Every move: position, direction, available options, visited count |
| `Information` | Maze started, maze completed, periodic stats summary |
| `Warning` | API errors that are retried, unexpected response formats |
| `Error` | Unrecoverable failures, API unreachable after retries |

**Example log events:**

```json
{"Level":"Debug","Message":"Moving {Direction} from {FromPosition} to {ToPosition}","Direction":"North","FromPosition":{"X":2,"Y":3},"ToPosition":{"X":2,"Y":2},"AvailableMoves":["North","East"],"VisitedCount":12}

{"Level":"Information","Message":"Maze {MazeId} solved in {MoveCount} moves ({ElapsedMs}ms)","MazeId":"550e8400-...","MoveCount":47,"ElapsedMs":1523}

{"Level":"Information","Message":"Stats: {MazesSolved} mazes, {TotalMoves} moves, avg {AvgMoves} moves/maze","MazesSolved":10,"TotalMoves":523,"AvgMoves":52.3}
```

## API Client

```csharp
public interface IMazeApiClient
{
    Task<MazeListResponse> GetMazesAsync(CancellationToken ct);
    Task<MazeResponse> CreateMazeAsync(int width, int height, CancellationToken ct);
    Task<SessionResponse> StartSessionAsync(Link startLink, CancellationToken ct);
    Task<SessionResponse> MoveAsync(Link moveLink, CancellationToken ct);
    Task<SessionResponse> GetSessionAsync(Link selfLink, CancellationToken ct);
}
```

**Design:**
- Link-based navigation - methods accept `Link` objects, not constructed URLs
- Entry point exception - `GetMazesAsync` uses configured base URL (the HATEOAS "bookmark")
- `IHttpClientFactory` for proper HttpClient lifecycle

**Error handling:**

| Scenario | Behavior |
|----------|----------|
| API unreachable | Retry 3x with exponential backoff, log error, wait before next maze |
| 4xx response | Log warning, abort current maze, start new one |
| 5xx response | Retry with backoff |
| Unexpected JSON | Log warning with body, abort current maze |

## Docker Configuration

**Dockerfile.solver:**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MazeOfHateoas.Solver/", "src/MazeOfHateoas.Solver/"]
COPY ["MazeOfHateoas.sln", "."]
RUN dotnet restore src/MazeOfHateoas.Solver/
RUN dotnet publish src/MazeOfHateoas.Solver/ -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MazeOfHateoas.Solver.dll"]
```

**docker-compose.solver.yml:**

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/mazes"]
      interval: 5s
      timeout: 3s
      retries: 5

  solver:
    build:
      context: .
      dockerfile: Dockerfile.solver
    depends_on:
      api:
        condition: service_healthy
    environment:
      - SOLVER_API_BASE_URL=http://api:8080
      - Serilog__MinimumLevel__Default=Information
```

**Usage:**

```bash
# Orchestrated (starts both)
docker compose -f docker-compose.solver.yml up --build

# Standalone (API running elsewhere)
docker compose -f docker-compose.solver.yml run --rm \
  -e SOLVER_API_BASE_URL=http://host:8080 solver
```

## Testing Strategy

Unit tests in `MazeOfHateoas.Solver.UnitTests/`, run in containers.

| Component | Test Focus |
|-----------|------------|
| `HateoasSolver` | Algorithm: visited tracking, backtracking, direction selection |
| `MazeApiClient` | Response parsing, link extraction, error mapping |
| `SolverSettings` | Configuration binding |

**Key test scenarios:**

- `Solver_WhenMultipleDirectionsAvailable_ChoosesUnvisited`
- `Solver_WhenDeadEnd_BacktracksToLastJunction`
- `Solver_WhenAllDirectionsVisited_ContinuesBacktracking`
- `Solver_WhenSessionCompleted_ReturnsSuccess`
- `ApiClient_WhenLinkClicked_UsesExactHref`
- `ApiClient_When5xxResponse_ThrowsRetryableException`

**Test approach:**
- Mock `IMazeApiClient` for solver tests with canned responses
- Mock `HttpMessageHandler` for API client tests
- Builder pattern for test data

**Execution:**

```bash
docker compose -f docker-compose.test.yml run --rm test \
  dotnet test --filter "FullyQualifiedName~Solver"
```
