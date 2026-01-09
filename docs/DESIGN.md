# Maze of HATEOAS - Design Document

## 1. Introduction

### 1.1 Purpose

Maze of HATEOAS is a RESTful API that demonstrates Hypermedia as the Engine of Application State (HATEOAS) principles through an interactive maze navigation experience. Players generate mazes and navigate through them by following hypermedia links—the API dynamically communicates available moves based on the player's current position and surrounding walls.

### 1.2 HATEOAS Concept

In a HATEOAS-compliant API, clients discover available actions through hypermedia links embedded in responses rather than hardcoding endpoints. For a maze, this means:

- When standing at a cell with an open path north, the response includes a link to move north
- When blocked by a wall, no link is provided for that direction
- The client needs no prior knowledge of the maze layout—it simply follows available links

### 1.3 Design Principles

This project adheres to the directives in `CLAUDE.md`:

| Principle | Application |
|-----------|-------------|
| **TDD** | All code written test-first; tests define behavior before implementation |
| **Pragmatic Programmer** | DRY, orthogonality, tracer bullets, reversibility |
| **12-Factor App** | Config via environment, stateless processes, dev/prod parity |
| **SOLID** | Single responsibility, open/closed, dependency inversion |
| **Containerized Execution** | All builds and tests run in Docker containers |

---

## 2. Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 8.0 LTS |
| Framework | ASP.NET Core | 8.0 |
| Testing | xUnit | 2.x |
| Mocking | Moq | 4.x |
| Containers | Docker | Latest |
| Build Orchestration | Docker Compose | Latest |

---

## 3. Architecture

### 3.1 Clean Architecture

The solution follows Clean Architecture with four layers:

```
maze-of-hateoas/
├── src/
│   ├── MazeOfHateoas.Api/              # Presentation Layer
│   │   ├── Controllers/
│   │   ├── Models/                     # API DTOs and link models
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   ├── MazeOfHateoas.Application/      # Application Layer
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── DTOs/
│   │
│   ├── MazeOfHateoas.Domain/           # Domain Layer
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Enums/
│   │
│   └── MazeOfHateoas.Infrastructure/   # Infrastructure Layer
│       ├── Persistence/
│       └── Services/
│
├── tests/
│   ├── MazeOfHateoas.UnitTests/
│   └── MazeOfHateoas.IntegrationTests/
│
├── Dockerfile
├── docker-compose.yml
├── docker-compose.test.yml
└── MazeOfHateoas.sln
```

### 3.2 Layer Responsibilities

**Domain Layer** (innermost, no dependencies)
- Pure C# entities and value objects
- Business rules and invariants
- No framework dependencies

**Application Layer** (depends on Domain)
- Use cases and business logic orchestration
- Interfaces for infrastructure concerns
- DTOs for data transfer

**Infrastructure Layer** (depends on Application)
- Implementation of persistence interfaces
- External service integrations
- In-memory maze storage

**API Layer** (depends on Application)
- HTTP request/response handling
- HATEOAS link generation
- Input validation and error responses

### 3.3 Dependency Flow

```
┌─────────────────────────────────────────────────────────┐
│                      API Layer                          │
│  Controllers, Link Generation, Request/Response Models  │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                      │
│         Use Cases, Interfaces, Service Logic            │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer                         │
│          Entities, Value Objects, Enums                 │
└─────────────────────────────────────────────────────────┘
                           ▲
                           │ implements interfaces from
┌──────────────────────────┴──────────────────────────────┐
│                 Infrastructure Layer                    │
│            In-Memory Storage, Services                  │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Domain Model

### 4.1 Entities

#### Maze

```csharp
public class Maze
{
    public Guid Id { get; }
    public int Width { get; }
    public int Height { get; }
    public Cell[,] Cells { get; }
    public Position Start { get; }
    public Position End { get; }
    public DateTime CreatedAt { get; }
}
```

#### MazeSession

```csharp
public class MazeSession
{
    public Guid Id { get; }
    public Guid MazeId { get; }
    public Position CurrentPosition { get; private set; }
    public SessionState State { get; private set; }
    public DateTime StartedAt { get; }

    public MoveResult Move(Direction direction, Maze maze);
}
```

### 4.2 Value Objects

#### Position

```csharp
public readonly record struct Position(int X, int Y)
{
    public Position Move(Direction direction) => direction switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.East => this with { X = X + 1 },
        Direction.West => this with { X = X - 1 },
        _ => this
    };
}
```

#### Cell

```csharp
public readonly record struct Cell(
    Position Position,
    bool HasNorthWall,
    bool HasSouthWall,
    bool HasEastWall,
    bool HasWestWall
)
{
    public bool CanMove(Direction direction) => direction switch
    {
        Direction.North => !HasNorthWall,
        Direction.South => !HasSouthWall,
        Direction.East => !HasEastWall,
        Direction.West => !HasWestWall,
        _ => false
    };
}
```

### 4.3 Enums

```csharp
public enum Direction { North, South, East, West }

public enum SessionState { InProgress, Completed }

public enum MoveResult { Success, Blocked, OutOfBounds, AlreadyCompleted }
```

---

## 5. API Design

### 5.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/mazes` | GET | List all available mazes |
| `/api/mazes` | POST | Generate a new maze |
| `/api/mazes/{mazeId}` | GET | Get maze details |
| `/api/mazes/{mazeId}/sessions` | POST | Start a new session (enter the maze) |
| `/api/mazes/{mazeId}/sessions/{sessionId}` | GET | Get current session state |
| `/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}` | POST | Move in a direction |

### 5.2 HATEOAS Link Model

```csharp
public class Link
{
    public string Href { get; init; }
    public string Rel { get; init; }
    public string Method { get; init; }
}

public abstract class HateoasResource
{
    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; } = new();
}
```

### 5.3 Response Examples

#### GET /api/mazes

```json
{
  "mazes": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "width": 10,
      "height": 10,
      "createdAt": "2025-01-09T10:30:00Z",
      "_links": {
        "self": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET" },
        "start": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST" }
      }
    }
  ],
  "_links": {
    "self": { "href": "/api/mazes", "rel": "self", "method": "GET" },
    "create": { "href": "/api/mazes", "rel": "create", "method": "POST" }
  }
}
```

#### POST /api/mazes

Request:
```json
{
  "width": 10,
  "height": 10
}
```

Response (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "width": 10,
  "height": 10,
  "start": { "x": 0, "y": 0 },
  "end": { "x": 9, "y": 9 },
  "createdAt": "2025-01-09T10:30:00Z",
  "_links": {
    "self": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET" },
    "start": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST" }
  }
}
```

#### GET /api/mazes/{mazeId}/sessions/{sessionId}

This is the core HATEOAS response—links vary based on available moves:

```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 2, "y": 3 },
  "state": "InProgress",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "north": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/north", "rel": "move", "method": "POST" },
    "east": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/east", "rel": "move", "method": "POST" }
  }
}
```

Note: Only `north` and `east` links appear because walls block south and west.

#### POST /api/mazes/{mazeId}/sessions/{sessionId}/move/north

Response (200 OK):
```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 2, "y": 2 },
  "state": "InProgress",
  "moveResult": "Success",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "south": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/south", "rel": "move", "method": "POST" },
    "west": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/west", "rel": "move", "method": "POST" }
  }
}
```

#### Session Completed (reached end)

```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 9, "y": 9 },
  "state": "Completed",
  "message": "Congratulations! You've completed the maze!",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "mazes": { "href": "/api/mazes", "rel": "collection", "method": "GET" },
    "newMaze": { "href": "/api/mazes", "rel": "create", "method": "POST" }
  }
}
```

---

## 6. Maze Generation Algorithm

### 6.1 Recursive Backtracking (Depth-First Search)

The maze is generated using the recursive backtracking algorithm, implemented iteratively with a stack to avoid recursion limits.

### 6.2 Algorithm Steps

```
1. Create a grid of cells, all walls intact
2. Choose starting cell, mark as visited, push to stack
3. While stack is not empty:
   a. Pop current cell from stack
   b. Get unvisited neighbors
   c. If unvisited neighbors exist:
      - Push current cell back to stack
      - Choose random unvisited neighbor
      - Remove wall between current and chosen
      - Mark chosen as visited
      - Push chosen to stack
4. Set start at (0,0) and end at (width-1, height-1)
```

### 6.3 Implementation

```csharp
public class MazeGenerator : IMazeGenerator
{
    private readonly Random _random;

    public MazeGenerator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public Maze Generate(int width, int height)
    {
        var cells = InitializeGrid(width, height);
        var visited = new bool[width, height];
        var stack = new Stack<Position>();

        var start = new Position(0, 0);
        visited[0, 0] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            var neighbors = GetUnvisitedNeighbors(current, visited, width, height);

            if (neighbors.Count > 0)
            {
                stack.Push(current);
                var chosen = neighbors[_random.Next(neighbors.Count)];
                RemoveWallBetween(cells, current, chosen);
                visited[chosen.X, chosen.Y] = true;
                stack.Push(chosen);
            }
        }

        return new Maze(
            Guid.NewGuid(),
            width,
            height,
            cells,
            start,
            new Position(width - 1, height - 1),
            DateTime.UtcNow
        );
    }
}
```

---

## 7. Storage

### 7.1 In-Memory Implementation

For the MVP, mazes and sessions are stored in `ConcurrentDictionary` for thread-safe access:

```csharp
public class InMemoryMazeRepository : IMazeRepository
{
    private readonly ConcurrentDictionary<Guid, Maze> _mazes = new();

    public Task<Maze?> GetByIdAsync(Guid id)
        => Task.FromResult(_mazes.GetValueOrDefault(id));

    public Task<IEnumerable<Maze>> GetAllAsync()
        => Task.FromResult<IEnumerable<Maze>>(_mazes.Values.ToList());

    public Task SaveAsync(Maze maze)
    {
        _mazes[maze.Id] = maze;
        return Task.CompletedTask;
    }
}
```

### 7.2 Repository Interfaces

```csharp
public interface IMazeRepository
{
    Task<Maze?> GetByIdAsync(Guid id);
    Task<IEnumerable<Maze>> GetAllAsync();
    Task SaveAsync(Maze maze);
}

public interface ISessionRepository
{
    Task<MazeSession?> GetByIdAsync(Guid mazeId, Guid sessionId);
    Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId);
    Task SaveAsync(MazeSession session);
}
```

---

## 8. Configuration (12-Factor)

### 8.1 Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Production |
| `MAZE_DEFAULT_WIDTH` | Default maze width | 10 |
| `MAZE_DEFAULT_HEIGHT` | Default maze height | 10 |
| `MAZE_MAX_WIDTH` | Maximum allowed width | 50 |
| `MAZE_MAX_HEIGHT` | Maximum allowed height | 50 |

### 8.2 Configuration Class

```csharp
public class MazeSettings
{
    public int DefaultWidth { get; set; } = 10;
    public int DefaultHeight { get; set; } = 10;
    public int MaxWidth { get; set; } = 50;
    public int MaxHeight { get; set; } = 50;
}
```

### 8.3 Program.cs Configuration

```csharp
builder.Services.Configure<MazeSettings>(options =>
{
    options.DefaultWidth = builder.Configuration.GetValue("MAZE_DEFAULT_WIDTH", 10);
    options.DefaultHeight = builder.Configuration.GetValue("MAZE_DEFAULT_HEIGHT", 10);
    options.MaxWidth = builder.Configuration.GetValue("MAZE_MAX_WIDTH", 50);
    options.MaxHeight = builder.Configuration.GetValue("MAZE_MAX_HEIGHT", 50);
});
```

---

## 9. Docker Configuration

### 9.1 Dockerfile

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

### 9.2 docker-compose.yml

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - MAZE_DEFAULT_WIDTH=10
      - MAZE_DEFAULT_HEIGHT=10
```

### 9.3 docker-compose.test.yml

```yaml
services:
  test:
    build:
      context: .
      dockerfile: Dockerfile.test
    volumes:
      - ./TestResults:/app/TestResults
```

### 9.4 Dockerfile.test

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src
COPY . .
RUN dotnet restore
ENTRYPOINT ["dotnet", "test", "--logger:trx", "--results-directory:/app/TestResults"]
```

---

## 10. Testing Strategy (TDD)

### 10.1 Test-First Workflow

Per CLAUDE.md, all code is written test-first:

1. Write a failing test that defines desired behavior
2. Run test to confirm it fails
3. Write minimal code to make test pass
4. Refactor while keeping tests green
5. Repeat

### 10.2 Test Categories

#### Unit Tests

- **Domain**: Maze, Cell, Position, Direction behavior
- **Application**: Service logic, use case orchestration
- **Maze Generation**: Algorithm correctness, randomness seeding

```csharp
public class CellTests
{
    [Theory]
    [InlineData(Direction.North, false, true)]
    [InlineData(Direction.North, true, false)]
    public void CanMove_ReturnsExpectedResult(Direction direction, bool hasWall, bool expected)
    {
        var cell = new Cell(new Position(0, 0), hasWall, false, false, false);
        Assert.Equal(expected, cell.CanMove(direction));
    }
}
```

#### Integration Tests

- **API Endpoints**: Full HTTP request/response cycle
- **HATEOAS Links**: Verify links are correct and navigable

```csharp
public class MazeApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateMaze_ReturnsCreatedWithLinks()
    {
        var response = await _client.PostAsJsonAsync("/api/mazes", new { Width = 5, Height = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var maze = await response.Content.ReadFromJsonAsync<MazeResponse>();
        Assert.Contains("self", maze.Links.Keys);
        Assert.Contains("start", maze.Links.Keys);
    }
}
```

### 10.3 Running Tests in Container

```bash
# Run all tests
docker compose -f docker-compose.test.yml up --build

# View results
cat TestResults/*.trx
```

---

## 11. Error Handling

### 11.1 Problem Details (RFC 7807)

All errors return standard Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Maze with ID '123' was not found",
  "instance": "/api/mazes/123"
}
```

### 11.2 Error Scenarios

| Scenario | Status | Type |
|----------|--------|------|
| Maze not found | 404 | NotFound |
| Session not found | 404 | NotFound |
| Invalid maze dimensions | 400 | BadRequest |
| Move blocked by wall | 400 | BadRequest |
| Session already completed | 400 | BadRequest |

---

## 12. Future Considerations

These are not in scope for MVP but noted for potential extension:

- **Persistent Storage**: EF Core with PostgreSQL/SQLite
- **Multiplayer**: Multiple sessions, race mode
- **Visualization**: ASCII or graphical maze rendering endpoint
- **Leaderboards**: Track completion times
- **Alternative Algorithms**: Prim's, Kruskal's, Wilson's

---

## 13. References

- [HATEOAS in ASP.NET Core - Code Maze](https://code-maze.com/hateoas-aspnet-core-web-api/)
- [12-Factor App](https://12factor.net/)
- [Maze Generation Algorithm - Wikipedia](https://en.wikipedia.org/wiki/Maze_generation_algorithm)
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
