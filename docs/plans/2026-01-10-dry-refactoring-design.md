# DRY Refactoring Design

## Problem

The codebase has 15+ distinct patterns of repeated code with 200+ total instances:

| Category | Files | Occurrences |
|----------|-------|-------------|
| ProblemDetails construction | MazesController, SessionsController | 12+ |
| Maze link generation | MazesController | 3 |
| MazeResponse mapping | MazesController | 2 |
| JSON parsing in tests | Integration tests | 160+ |
| Test data builders | Unit tests | 3 duplicate methods |
| Mock repositories | Unit tests | 2 duplicate classes |

## Design Decisions

1. **ProblemDetails** → Static factory methods
2. **Maze links/responses** → IMazeLinkGenerator service (mirrors ISessionLinkGenerator)
3. **Test helpers** → Shared utility classes

## Implementation

### 1. ProblemDetailsFactory

```csharp
// src/MazeOfHateoas.Api/Helpers/ProblemDetailsFactory.cs
public static class ProblemDetailsFactory
{
    private const string BadRequestType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    private const string NotFoundType = "https://tools.ietf.org/html/rfc7231#section-6.5.4";

    public static ProblemDetails BadRequest(string detail, string instance) => new()
    {
        Type = BadRequestType,
        Title = "Bad Request",
        Status = 400,
        Detail = detail,
        Instance = instance
    };

    public static ProblemDetails NotFound(string detail, string instance) => new()
    {
        Type = NotFoundType,
        Title = "Not Found",
        Status = 404,
        Detail = detail,
        Instance = instance
    };
}
```

### 2. IMazeLinkGenerator Service

```csharp
// src/MazeOfHateoas.Application/Services/IMazeLinkGenerator.cs
public interface IMazeLinkGenerator
{
    Dictionary<string, LinkDto> GenerateMazeLinks(Guid mazeId);
    Dictionary<string, LinkDto> GenerateListLinks();
}

// src/MazeOfHateoas.Api/Services/MazeLinkGenerator.cs
public class MazeLinkGenerator : IMazeLinkGenerator
{
    public Dictionary<string, LinkDto> GenerateMazeLinks(Guid mazeId) => new()
    {
        ["self"] = new LinkDto($"/api/mazes/{mazeId}", "self", "GET"),
        ["start"] = new LinkDto($"/api/mazes/{mazeId}/sessions", "start", "POST")
    };

    public Dictionary<string, LinkDto> GenerateListLinks() => new()
    {
        ["self"] = new LinkDto("/api/mazes", "self", "GET"),
        ["create"] = new LinkDto("/api/mazes", "create", "POST")
    };
}
```

### 3. MazesController Helper Method

```csharp
// Add to MazesController
private MazeResponse BuildMazeResponse(Maze maze)
{
    var links = _linkGenerator.GenerateMazeLinks(maze.Id)
        .ToDictionary(kvp => kvp.Key, kvp => (Link)kvp.Value);

    return new MazeResponse
    {
        Id = maze.Id,
        Width = maze.Width,
        Height = maze.Height,
        Start = new PositionDto(maze.Start.X, maze.Start.Y),
        End = new PositionDto(maze.End.X, maze.End.Y),
        CreatedAt = maze.CreatedAt,
        Links = links
    };
}

private MazeSummaryResponse BuildMazeSummaryResponse(Maze maze)
{
    var links = _linkGenerator.GenerateMazeLinks(maze.Id)
        .ToDictionary(kvp => kvp.Key, kvp => (Link)kvp.Value);

    return new MazeSummaryResponse
    {
        Id = maze.Id,
        Width = maze.Width,
        Height = maze.Height,
        CreatedAt = maze.CreatedAt,
        Links = links
    };
}
```

### 4. Test Utilities

```csharp
// tests/MazeOfHateoas.IntegrationTests/Helpers/TestHelpers.cs
public static class TestHelpers
{
    public static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    public static void AssertLink(JsonElement links, string key,
        string expectedHref, string expectedRel, string expectedMethod)
    {
        var link = links.GetProperty(key);
        Assert.Equal(expectedHref, link.GetProperty("href").GetString());
        Assert.Equal(expectedRel, link.GetProperty("rel").GetString());
        Assert.Equal(expectedMethod, link.GetProperty("method").GetString());
    }

    public static void AssertProblemDetails(JsonElement root,
        string expectedTitle, int expectedStatus)
    {
        Assert.Equal(expectedTitle, root.GetProperty("title").GetString());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
    }
}

// tests/MazeOfHateoas.UnitTests/Helpers/TestDataBuilders.cs
public static class TestDataBuilders
{
    public static Maze CreateTestMaze(int width = 3, int height = 3, bool allCellsOpen = true)
    {
        var cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = allCellsOpen
                    ? new Cell(new Position(x, y), false, false, false, false)
                    : new Cell(new Position(x, y), true, true, true, true);
            }
        }
        return new Maze(Guid.NewGuid(), width, height, cells,
            new Position(0, 0), new Position(width - 1, height - 1), DateTime.UtcNow);
    }

    public static MazeSession CreateTestSession(Guid mazeId, Position? position = null)
    {
        return new MazeSession(Guid.NewGuid(), mazeId, position ?? new Position(0, 0));
    }
}

// tests/MazeOfHateoas.UnitTests/Helpers/TestRepositories.cs
public class TestMazeRepository : IMazeRepository
{
    private readonly Dictionary<Guid, Maze> _mazes = new();

    public Task<Maze?> GetByIdAsync(Guid id) =>
        Task.FromResult(_mazes.GetValueOrDefault(id));

    public Task<IEnumerable<Maze>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Maze>>(_mazes.Values.ToList());

    public Task SaveAsync(Maze maze)
    {
        _mazes[maze.Id] = maze;
        return Task.CompletedTask;
    }

    public void Add(Maze maze) => _mazes[maze.Id] = maze;
}

public class TestSessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, MazeSession> _sessions = new();

    public Task<MazeSession?> GetByIdAsync(Guid id) =>
        Task.FromResult(_sessions.GetValueOrDefault(id));

    public Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId) =>
        Task.FromResult<IEnumerable<MazeSession>>(
            _sessions.Values.Where(s => s.MazeId == mazeId).ToList());

    public Task SaveAsync(MazeSession session)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public void Add(MazeSession session) => _sessions[session.Id] = session;
}
```

## Files to Create

| File | Purpose |
|------|---------|
| `src/MazeOfHateoas.Api/Helpers/ProblemDetailsFactory.cs` | Static factory for error responses |
| `src/MazeOfHateoas.Application/Services/IMazeLinkGenerator.cs` | Interface for maze link generation |
| `src/MazeOfHateoas.Api/Services/MazeLinkGenerator.cs` | Implementation of link generation |
| `tests/MazeOfHateoas.IntegrationTests/Helpers/TestHelpers.cs` | JSON parsing and assertion helpers |
| `tests/MazeOfHateoas.UnitTests/Helpers/TestDataBuilders.cs` | Test data factory methods |
| `tests/MazeOfHateoas.UnitTests/Helpers/TestRepositories.cs` | Shared test repository implementations |

## Files to Modify

| File | Changes |
|------|---------|
| `MazesController.cs` | Inject IMazeLinkGenerator, use ProblemDetailsFactory, add BuildMazeResponse |
| `SessionsController.cs` | Use ProblemDetailsFactory |
| `Program.cs` | Register MazeLinkGenerator |
| Integration tests | Use TestHelpers for JSON operations |
| Unit tests | Use TestDataBuilders and TestRepositories |

## DRY Impact

| Before | After |
|--------|-------|
| 12+ ProblemDetails blocks | 2 factory methods |
| 3 maze link dictionaries | 1 service method |
| 2 MazeResponse constructions | 1 helper method |
| 160+ JSON parsing instances | 3 utility methods |
| 2 duplicate test repository classes | 1 shared file |
