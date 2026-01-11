# DRY Refactoring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Eliminate 200+ instances of repeated code by introducing factory methods, services, and shared test utilities.

**Architecture:** Static factory for ProblemDetails, injectable IMazeLinkGenerator service (mirroring ISessionLinkGenerator), shared test utilities for JSON parsing and test data creation.

**Tech Stack:** ASP.NET Core 8, xUnit, Clean Architecture layers

---

## Task 1: ProblemDetailsFactory - Tests

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Helpers/ProblemDetailsFactoryTests.cs`

**Step 1: Write the failing tests**

```csharp
using MazeOfHateoas.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MazeOfHateoas.UnitTests.Helpers;

public class ProblemDetailsFactoryTests
{
    [Fact]
    public void BadRequest_ReturnsCorrectProblemDetails()
    {
        var result = ProblemDetailsFactory.BadRequest("Test detail", "/api/test");

        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", result.Type);
        Assert.Equal("Bad Request", result.Title);
        Assert.Equal(400, result.Status);
        Assert.Equal("Test detail", result.Detail);
        Assert.Equal("/api/test", result.Instance);
    }

    [Fact]
    public void NotFound_ReturnsCorrectProblemDetails()
    {
        var result = ProblemDetailsFactory.NotFound("Not found detail", "/api/items/123");

        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", result.Type);
        Assert.Equal("Not Found", result.Title);
        Assert.Equal(404, result.Status);
        Assert.Equal("Not found detail", result.Detail);
        Assert.Equal("/api/items/123", result.Instance);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~ProblemDetailsFactoryTests"`
Expected: FAIL - "The type or namespace name 'ProblemDetailsFactory' could not be found"

**Step 3: Commit failing test**

```bash
git add tests/MazeOfHateoas.UnitTests/Helpers/ProblemDetailsFactoryTests.cs
git commit -m "test: add ProblemDetailsFactory unit tests (red)"
```

---

## Task 2: ProblemDetailsFactory - Implementation

**Files:**
- Create: `src/MazeOfHateoas.Api/Helpers/ProblemDetailsFactory.cs`

**Step 1: Write minimal implementation**

```csharp
using Microsoft.AspNetCore.Mvc;

namespace MazeOfHateoas.Api.Helpers;

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

**Step 2: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~ProblemDetailsFactoryTests"`
Expected: PASS (2 tests)

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Api/Helpers/ProblemDetailsFactory.cs
git commit -m "feat: add ProblemDetailsFactory for DRY error responses"
```

---

## Task 3: IMazeLinkGenerator Interface

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Services/MazeLinkGeneratorTests.cs`
- Create: `src/MazeOfHateoas.Application/Services/IMazeLinkGenerator.cs`

**Step 1: Write the failing tests**

```csharp
using MazeOfHateoas.Api.Services;
using Xunit;

namespace MazeOfHateoas.UnitTests.Services;

public class MazeLinkGeneratorTests
{
    private readonly MazeLinkGenerator _generator = new();

    [Fact]
    public void GenerateMazeLinks_ReturnsCorrectSelfLink()
    {
        var mazeId = Guid.NewGuid();

        var links = _generator.GenerateMazeLinks(mazeId);

        Assert.True(links.ContainsKey("self"));
        Assert.Equal($"/api/mazes/{mazeId}", links["self"].Href);
        Assert.Equal("self", links["self"].Rel);
        Assert.Equal("GET", links["self"].Method);
    }

    [Fact]
    public void GenerateMazeLinks_ReturnsCorrectStartLink()
    {
        var mazeId = Guid.NewGuid();

        var links = _generator.GenerateMazeLinks(mazeId);

        Assert.True(links.ContainsKey("start"));
        Assert.Equal($"/api/mazes/{mazeId}/sessions", links["start"].Href);
        Assert.Equal("start", links["start"].Rel);
        Assert.Equal("POST", links["start"].Method);
    }

    [Fact]
    public void GenerateListLinks_ReturnsCorrectSelfLink()
    {
        var links = _generator.GenerateListLinks();

        Assert.True(links.ContainsKey("self"));
        Assert.Equal("/api/mazes", links["self"].Href);
        Assert.Equal("self", links["self"].Rel);
        Assert.Equal("GET", links["self"].Method);
    }

    [Fact]
    public void GenerateListLinks_ReturnsCorrectCreateLink()
    {
        var links = _generator.GenerateListLinks();

        Assert.True(links.ContainsKey("create"));
        Assert.Equal("/api/mazes", links["create"].Href);
        Assert.Equal("create", links["create"].Rel);
        Assert.Equal("POST", links["create"].Method);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeLinkGeneratorTests"`
Expected: FAIL - "The type or namespace name 'MazeLinkGenerator' could not be found"

**Step 3: Commit failing test**

```bash
git add tests/MazeOfHateoas.UnitTests/Services/MazeLinkGeneratorTests.cs
git commit -m "test: add MazeLinkGenerator unit tests (red)"
```

---

## Task 4: IMazeLinkGenerator Implementation

**Files:**
- Create: `src/MazeOfHateoas.Application/Services/IMazeLinkGenerator.cs`
- Create: `src/MazeOfHateoas.Api/Services/MazeLinkGenerator.cs`

**Step 1: Create the interface**

```csharp
using MazeOfHateoas.Application.Services;

namespace MazeOfHateoas.Application.Services;

public interface IMazeLinkGenerator
{
    Dictionary<string, LinkDto> GenerateMazeLinks(Guid mazeId);
    Dictionary<string, LinkDto> GenerateListLinks();
}
```

**Step 2: Create the implementation**

```csharp
using MazeOfHateoas.Application.Services;

namespace MazeOfHateoas.Api.Services;

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

**Step 3: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeLinkGeneratorTests"`
Expected: PASS (4 tests)

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Application/Services/IMazeLinkGenerator.cs src/MazeOfHateoas.Api/Services/MazeLinkGenerator.cs
git commit -m "feat: add IMazeLinkGenerator service for DRY link generation"
```

---

## Task 5: Register MazeLinkGenerator in DI

**Files:**
- Modify: `src/MazeOfHateoas.Api/Program.cs`

**Step 1: Add registration after ISessionLinkGenerator**

Find line with `builder.Services.AddSingleton<ISessionLinkGenerator, SessionLinkGenerator>();`
Add after it:
```csharp
builder.Services.AddSingleton<IMazeLinkGenerator, MazeLinkGenerator>();
```

Also add using at top:
```csharp
using MazeOfHateoas.Application.Services;
```

**Step 2: Run all tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml up --build`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Api/Program.cs
git commit -m "chore: register MazeLinkGenerator in DI container"
```

---

## Task 6: Refactor MazesController - ProblemDetails

**Files:**
- Modify: `src/MazeOfHateoas.Api/Controllers/MazesController.cs`

**Step 1: Add using statement**

Add at top of file:
```csharp
using MazeOfHateoas.Api.Helpers;
```

**Step 2: Replace all ProblemDetails constructions**

Replace lines 58-66:
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    "Width must be a positive integer", "/api/mazes"));
```

Replace lines 70-78:
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    "Height must be a positive integer", "/api/mazes"));
```

Replace lines 83-91:
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    $"Width cannot exceed {_settings.MaxWidth}", "/api/mazes"));
```

Replace lines 95-103:
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    $"Height cannot exceed {_settings.MaxHeight}", "/api/mazes"));
```

Replace lines 189-196:
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Maze with ID '{id}' was not found", $"/api/mazes/{id}"));
```

**Step 3: Run tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazesControllerTests"`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Api/Controllers/MazesController.cs
git commit -m "refactor: use ProblemDetailsFactory in MazesController"
```

---

## Task 7: Refactor MazesController - Link Generator

**Files:**
- Modify: `src/MazeOfHateoas.Api/Controllers/MazesController.cs`

**Step 1: Add field and constructor parameter**

Add field after `_logger`:
```csharp
private readonly IMazeLinkGenerator _linkGenerator;
```

Add parameter and assignment in constructor:
```csharp
public MazesController(
    IMazeGenerator mazeGenerator,
    IMazeRepository mazeRepository,
    IOptions<MazeSettings> settings,
    ILogger<MazesController> logger,
    IMazeLinkGenerator linkGenerator)
{
    _mazeGenerator = mazeGenerator;
    _mazeRepository = mazeRepository;
    _settings = settings.Value;
    _logger = logger;
    _linkGenerator = linkGenerator;
}
```

**Step 2: Add helper methods at end of class**

```csharp
private MazeResponse BuildMazeResponse(Maze maze)
{
    var links = _linkGenerator.GenerateMazeLinks(maze.Id)
        .ToDictionary(kvp => kvp.Key, kvp => new Link(kvp.Value.Href, kvp.Value.Rel, kvp.Value.Method));

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
        .ToDictionary(kvp => kvp.Key, kvp => new Link(kvp.Value.Href, kvp.Value.Rel, kvp.Value.Method));

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

**Step 3: Replace response building in CreateMaze**

Replace lines 111-124 with:
```csharp
var response = BuildMazeResponse(maze);
```

**Step 4: Replace response building in GetAllMazes**

Replace lines 146-163 with:
```csharp
var response = new MazeListResponse
{
    Mazes = mazes.Select(BuildMazeSummaryResponse).ToList(),
    Links = _linkGenerator.GenerateListLinks()
        .ToDictionary(kvp => kvp.Key, kvp => new Link(kvp.Value.Href, kvp.Value.Rel, kvp.Value.Method))
};
```

**Step 5: Replace response building in GetMaze**

Replace lines 199-212 with:
```csharp
var response = BuildMazeResponse(maze);
```

**Step 6: Run tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazesController"`
Expected: All tests PASS

**Step 7: Commit**

```bash
git add src/MazeOfHateoas.Api/Controllers/MazesController.cs
git commit -m "refactor: use IMazeLinkGenerator and helper methods in MazesController"
```

---

## Task 8: Refactor SessionsController - ProblemDetails

**Files:**
- Modify: `src/MazeOfHateoas.Api/Controllers/SessionsController.cs`

**Step 1: Add using statement**

Add at top of file:
```csharp
using MazeOfHateoas.Api.Helpers;
```

**Step 2: Replace all ProblemDetails constructions**

Replace CreateSession not found (lines 60-67):
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Maze with ID '{mazeId}' was not found",
    $"/api/mazes/{mazeId}/sessions"));
```

Replace GetSession maze not found (lines 104-111):
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Maze with ID '{mazeId}' was not found",
    $"/api/mazes/{mazeId}/sessions/{sessionId}"));
```

Replace GetSession session not found (lines 118-125):
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Session with ID '{sessionId}' was not found",
    $"/api/mazes/{mazeId}/sessions/{sessionId}"));
```

Replace Move invalid direction (lines 160-167):
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    $"Invalid direction '{direction}'. Valid directions are: north, south, east, west",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

Replace Move maze not found (lines 173-180):
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Maze with ID '{mazeId}' was not found",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

Replace Move session not found (lines 186-193):
```csharp
return NotFound(ProblemDetailsFactory.NotFound(
    $"Session with ID '{sessionId}' was not found",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

Replace Move already completed (lines 200-207):
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    "Cannot move - session is already completed",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

Replace Move blocked (lines 212-219):
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    $"Cannot move {direction} - blocked by wall",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

Replace Move out of bounds (lines 224-231):
```csharp
return BadRequest(ProblemDetailsFactory.BadRequest(
    $"Cannot move {direction} - out of bounds",
    $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"));
```

**Step 3: Run tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~SessionsController"`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Api/Controllers/SessionsController.cs
git commit -m "refactor: use ProblemDetailsFactory in SessionsController"
```

---

## Task 9: Integration Test Helpers

**Files:**
- Create: `tests/MazeOfHateoas.IntegrationTests/Helpers/TestHelpers.cs`

**Step 1: Create test helpers**

```csharp
using System.Text.Json;
using Xunit;

namespace MazeOfHateoas.IntegrationTests.Helpers;

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
        Assert.True(links.TryGetProperty(key, out var link), $"Link '{key}' not found");
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

    public static string GetId(JsonDocument json) =>
        json.RootElement.GetProperty("id").GetString()!;

    public static JsonElement GetLinks(JsonDocument json) =>
        json.RootElement.GetProperty("_links");
}
```

**Step 2: Run all tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml up --build`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add tests/MazeOfHateoas.IntegrationTests/Helpers/TestHelpers.cs
git commit -m "feat: add integration test helpers for JSON parsing"
```

---

## Task 10: Unit Test Helpers - TestDataBuilders

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Helpers/TestDataBuilders.cs`

**Step 1: Create test data builders**

```csharp
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Helpers;

public static class TestDataBuilders
{
    public static Maze CreateTestMaze(
        int width = 3,
        int height = 3,
        bool allCellsOpen = true,
        Guid? id = null)
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

        return new Maze(
            id ?? Guid.NewGuid(),
            width,
            height,
            cells,
            new Position(0, 0),
            new Position(width - 1, height - 1),
            DateTime.UtcNow);
    }

    public static MazeSession CreateTestSession(
        Guid mazeId,
        Position? position = null,
        Guid? id = null)
    {
        return new MazeSession(
            id ?? Guid.NewGuid(),
            mazeId,
            position ?? new Position(0, 0));
    }
}
```

**Step 2: Run all tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml up --build`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Helpers/TestDataBuilders.cs
git commit -m "feat: add TestDataBuilders for unit tests"
```

---

## Task 11: Unit Test Helpers - TestRepositories

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Helpers/TestRepositories.cs`

**Step 1: Create shared test repositories**

```csharp
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Helpers;

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

**Step 2: Run all tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml up --build`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Helpers/TestRepositories.cs
git commit -m "feat: add shared TestRepositories for unit tests"
```

---

## Task 12: Migrate MazesControllerLoggingTests to Use Helpers

**Files:**
- Modify: `tests/MazeOfHateoas.UnitTests/Controllers/MazesControllerLoggingTests.cs`

**Step 1: Add using statement**

```csharp
using MazeOfHateoas.UnitTests.Helpers;
```

**Step 2: Remove duplicate TestMazeRepository class**

Delete the private `TestMazeRepository` class at end of file.

**Step 3: Replace inline maze creation with TestDataBuilders**

Replace any `CreateTestMaze` method calls with `TestDataBuilders.CreateTestMaze(...)`.

**Step 4: Run tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazesControllerLoggingTests"`
Expected: All tests PASS

**Step 5: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Controllers/MazesControllerLoggingTests.cs
git commit -m "refactor: use shared test helpers in MazesControllerLoggingTests"
```

---

## Task 13: Migrate SessionsControllerLoggingTests to Use Helpers

**Files:**
- Modify: `tests/MazeOfHateoas.UnitTests/Controllers/SessionsControllerLoggingTests.cs`

**Step 1: Add using statement**

```csharp
using MazeOfHateoas.UnitTests.Helpers;
```

**Step 2: Remove duplicate test repository classes and maze builders**

Delete:
- `TestMazeRepository` class
- `TestSessionRepository` class
- `CreateTestMaze` method
- `CreateTestMazeWithOpenPaths` method
- `CreateMazeForCompletion` method

**Step 3: Replace with shared helpers**

Use `TestMazeRepository`, `TestSessionRepository` from Helpers namespace.
Use `TestDataBuilders.CreateTestMaze(...)` for maze creation.

**Step 4: Run tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~SessionsControllerLoggingTests"`
Expected: All tests PASS

**Step 5: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Controllers/SessionsControllerLoggingTests.cs
git commit -m "refactor: use shared test helpers in SessionsControllerLoggingTests"
```

---

## Task 14: Run Full Test Suite and Verify

**Step 1: Run complete test suite**

Run: `docker compose -f docker-compose.test.yml up --build`
Expected: All tests PASS

**Step 2: Verify build works**

Run: `docker compose build`
Expected: Build SUCCESS

**Step 3: Final commit with any cleanup**

```bash
git status
# If any uncommitted changes, commit them
```

---

## Summary

| Task | Component | Type |
|------|-----------|------|
| 1-2 | ProblemDetailsFactory | Create + Test |
| 3-4 | IMazeLinkGenerator | Create + Test |
| 5 | DI Registration | Config |
| 6-7 | MazesController | Refactor |
| 8 | SessionsController | Refactor |
| 9-11 | Test Helpers | Create |
| 12-13 | Logging Tests | Migrate |
| 14 | Verification | Validate |
