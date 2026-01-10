# Structured JSON Logging Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add structured JSON logging using Serilog with configurable log levels via appsettings.json and environment variable override.

**Architecture:** Serilog replaces default ASP.NET Core logging. JSON output via CompactJsonFormatter to stdout. Request logging middleware captures HTTP traffic. Controllers inject ILogger<T> for application events.

**Tech Stack:** Serilog.AspNetCore, Serilog.Formatting.Compact, Microsoft.Extensions.Logging.Testing (for tests)

---

## Task 1: Add NuGet Packages

**Files:**
- Modify: `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj`
- Modify: `tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj`

**Step 1: Add Serilog packages to API project**

Edit `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj`, add after the existing PackageReference items:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Formatting.Compact" Version="2.0.0" />
```

**Step 2: Add testing package to unit tests project**

Edit `tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj`, add after the existing PackageReference items:

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
```

**Step 3: Verify packages restore**

Run: `docker compose -f docker-compose.test.yml build`

Expected: Build succeeds with new packages restored

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj
git commit -m "feat(logging): add Serilog and logging test packages"
```

---

## Task 2: Configure Serilog in appsettings

**Files:**
- Modify: `src/MazeOfHateoas.Api/appsettings.json`
- Modify: `src/MazeOfHateoas.Api/appsettings.Development.json`

**Step 1: Update appsettings.json**

Replace the entire contents with:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "AllowedHosts": "*"
}
```

**Step 2: Update appsettings.Development.json**

Replace the entire contents with:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Api/appsettings.json src/MazeOfHateoas.Api/appsettings.Development.json
git commit -m "feat(logging): add Serilog configuration to appsettings"
```

---

## Task 3: Set Up Serilog in Program.cs

**Files:**
- Modify: `src/MazeOfHateoas.Api/Program.cs`

**Step 1: Add using statements at top of file**

Add after existing using statements:

```csharp
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
```

**Step 2: Configure Serilog before builder creation**

Add immediately after the using statements (before `var builder = WebApplication.CreateBuilder(args);`):

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();
```

**Step 3: Add UseSerilog to builder**

Add after `var builder = WebApplication.CreateBuilder(args);`:

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MazeOfHateoas")
    .WriteTo.Console(new CompactJsonFormatter()));
```

**Step 4: Add request logging middleware**

Add after `app.UseExceptionHandler(...)` block and before `app.UseSwagger()`:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };
    options.GetLevel = (httpContext, elapsed, ex) =>
        httpContext.Request.Path == "/health"
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
});
```

**Step 5: Verify build succeeds**

Run: `docker compose -f docker-compose.test.yml build`

Expected: Build succeeds

**Step 6: Run tests to verify no regressions**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: All 178 tests pass

**Step 7: Commit**

```bash
git add src/MazeOfHateoas.Api/Program.cs
git commit -m "feat(logging): configure Serilog with JSON output and request logging"
```

---

## Task 4: Add Exception Logging

**Files:**
- Modify: `src/MazeOfHateoas.Api/Program.cs`

**Step 1: Update exception handler to log errors**

Replace the existing `app.UseExceptionHandler(...)` block with:

```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

**Step 2: Add using for IExceptionHandlerFeature**

Add to using statements if not present:

```csharp
using Microsoft.AspNetCore.Diagnostics;
```

**Step 3: Run tests to verify no regressions**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: All 178 tests pass

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Api/Program.cs
git commit -m "feat(logging): add exception logging to global error handler"
```

---

## Task 5: Add Logging to MazesController - Tests First

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Controllers/MazesControllerLoggingTests.cs`
- Modify: `src/MazeOfHateoas.Api/Controllers/MazesController.cs`

**Step 1: Create test file**

Create `tests/MazeOfHateoas.UnitTests/Controllers/MazesControllerLoggingTests.cs`:

```csharp
using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.UnitTests.Controllers;

public class MazesControllerLoggingTests
{
    private readonly FakeLogger<MazesController> _logger;
    private readonly MazesController _controller;
    private readonly IMazeRepository _mazeRepository;
    private readonly IMazeGenerator _mazeGenerator;

    public MazesControllerLoggingTests()
    {
        _logger = new FakeLogger<MazesController>();
        _mazeRepository = new TestMazeRepository();
        _mazeGenerator = new TestMazeGenerator();
        var settings = Options.Create(new MazeSettings
        {
            DefaultWidth = 10,
            DefaultHeight = 10,
            MaxWidth = 50,
            MaxHeight = 50
        });
        _controller = new MazesController(_mazeGenerator, _mazeRepository, settings, _logger);
    }

    [Fact]
    public async Task CreateMaze_LogsInformationWithMazeDetails()
    {
        var request = new CreateMazeRequest { Width = 5, Height = 5 };

        await _controller.CreateMaze(request);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Maze created", logEntry.Message);
    }

    [Fact]
    public async Task GetMaze_WhenNotFound_LogsWarning()
    {
        var nonExistentId = Guid.NewGuid();

        await _controller.GetMaze(nonExistentId);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Contains("not found", logEntry.Message);
    }

    private class TestMazeRepository : IMazeRepository
    {
        private readonly Dictionary<Guid, Maze> _mazes = new();

        public Task<Maze?> GetByIdAsync(Guid id) =>
            Task.FromResult(_mazes.GetValueOrDefault(id));

        public Task<IEnumerable<Maze>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Maze>>(_mazes.Values);

        public Task SaveAsync(Maze maze)
        {
            _mazes[maze.Id] = maze;
            return Task.CompletedTask;
        }
    }

    private class TestMazeGenerator : IMazeGenerator
    {
        public Maze Generate(int width, int height)
        {
            var cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new Cell(new Position(x, y), true, true, true, true);
                }
            }
            return new Maze(Guid.NewGuid(), width, height, cells,
                new Position(0, 0), new Position(width - 1, height - 1), DateTime.UtcNow);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: FAIL - MazesController constructor doesn't accept ILogger parameter

**Step 3: Add ILogger to MazesController**

Modify `src/MazeOfHateoas.Api/Controllers/MazesController.cs`:

Add using statement:
```csharp
using Microsoft.Extensions.Logging;
```

Add field after existing fields:
```csharp
private readonly ILogger<MazesController> _logger;
```

Update constructor:
```csharp
public MazesController(
    IMazeGenerator mazeGenerator,
    IMazeRepository mazeRepository,
    IOptions<MazeSettings> settings,
    ILogger<MazesController> logger)
{
    _mazeGenerator = mazeGenerator;
    _mazeRepository = mazeRepository;
    _settings = settings.Value;
    _logger = logger;
}
```

Add logging in CreateMaze method, after `await _mazeRepository.SaveAsync(maze);`:
```csharp
_logger.LogInformation("Maze created: {MazeId} ({Width}x{Height})",
    maze.Id, width, height);
```

Add logging in GetMaze method, before `return NotFound(...)`:
```csharp
_logger.LogWarning("Maze not found: {MazeId}", id);
```

**Step 4: Run tests to verify they pass**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: All tests pass (180 tests - 2 new)

**Step 5: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Controllers/MazesControllerLoggingTests.cs src/MazeOfHateoas.Api/Controllers/MazesController.cs
git commit -m "feat(logging): add logging to MazesController with tests"
```

---

## Task 6: Add Logging to SessionsController - Tests First

**Files:**
- Create: `tests/MazeOfHateoas.UnitTests/Controllers/SessionsControllerLoggingTests.cs`
- Modify: `src/MazeOfHateoas.Api/Controllers/SessionsController.cs`

**Step 1: Create test file**

Create `tests/MazeOfHateoas.UnitTests/Controllers/SessionsControllerLoggingTests.cs`:

```csharp
using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace MazeOfHateoas.UnitTests.Controllers;

public class SessionsControllerLoggingTests
{
    private readonly FakeLogger<SessionsController> _logger;
    private readonly SessionsController _controller;
    private readonly TestMazeRepository _mazeRepository;
    private readonly TestSessionRepository _sessionRepository;

    public SessionsControllerLoggingTests()
    {
        _logger = new FakeLogger<SessionsController>();
        _mazeRepository = new TestMazeRepository();
        _sessionRepository = new TestSessionRepository();
        var linkGenerator = new SessionLinkGenerator();
        _controller = new SessionsController(_mazeRepository, _sessionRepository, linkGenerator, _logger);
    }

    [Fact]
    public async Task CreateSession_LogsInformationWithSessionDetails()
    {
        var maze = CreateTestMaze();
        await _mazeRepository.SaveAsync(maze);

        await _controller.CreateSession(maze.Id);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Session started", logEntry.Message);
    }

    [Fact]
    public async Task GetSession_WhenSessionNotFound_LogsWarning()
    {
        var maze = CreateTestMaze();
        await _mazeRepository.SaveAsync(maze);

        await _controller.GetSession(maze.Id, Guid.NewGuid());

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Contains("not found", logEntry.Message);
    }

    [Fact]
    public async Task Move_WhenSuccessful_LogsInformation()
    {
        var maze = CreateTestMazeWithOpenPaths();
        await _mazeRepository.SaveAsync(maze);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "south");

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Move", logEntry.Message);
    }

    [Fact]
    public async Task Move_WhenMazeCompleted_LogsCompletion()
    {
        var maze = CreateMazeForCompletion();
        await _mazeRepository.SaveAsync(maze);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "east");

        var logs = _logger.Collector.GetSnapshot();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message.Contains("completed"));
    }

    private static Maze CreateTestMaze()
    {
        var cells = new Cell[5, 5];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), true, true, true, true);
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, new Position(0, 0), new Position(4, 4), DateTime.UtcNow);
    }

    private static Maze CreateTestMazeWithOpenPaths()
    {
        var cells = new Cell[5, 5];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), false, false, false, false);
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, new Position(0, 0), new Position(4, 4), DateTime.UtcNow);
    }

    private static Maze CreateMazeForCompletion()
    {
        var cells = new Cell[2, 1];
        cells[0, 0] = new Cell(new Position(0, 0), true, true, false, true);
        cells[1, 0] = new Cell(new Position(1, 0), true, true, true, false);
        return new Maze(Guid.NewGuid(), 2, 1, cells, new Position(0, 0), new Position(1, 0), DateTime.UtcNow);
    }

    private class TestMazeRepository : IMazeRepository
    {
        private readonly Dictionary<Guid, Maze> _mazes = new();

        public Task<Maze?> GetByIdAsync(Guid id) =>
            Task.FromResult(_mazes.GetValueOrDefault(id));

        public Task<IEnumerable<Maze>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Maze>>(_mazes.Values);

        public Task SaveAsync(Maze maze)
        {
            _mazes[maze.Id] = maze;
            return Task.CompletedTask;
        }
    }

    private class TestSessionRepository : ISessionRepository
    {
        private readonly Dictionary<Guid, MazeSession> _sessions = new();

        public Task<MazeSession?> GetByIdAsync(Guid id) =>
            Task.FromResult(_sessions.GetValueOrDefault(id));

        public Task SaveAsync(MazeSession session)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: FAIL - SessionsController constructor doesn't accept ILogger parameter

**Step 3: Add ILogger to SessionsController**

Modify `src/MazeOfHateoas.Api/Controllers/SessionsController.cs`:

Add using statement:
```csharp
using Microsoft.Extensions.Logging;
```

Add field after existing fields:
```csharp
private readonly ILogger<SessionsController> _logger;
```

Update constructor:
```csharp
public SessionsController(
    IMazeRepository mazeRepository,
    ISessionRepository sessionRepository,
    ISessionLinkGenerator linkGenerator,
    ILogger<SessionsController> logger)
{
    _mazeRepository = mazeRepository;
    _sessionRepository = sessionRepository;
    _linkGenerator = linkGenerator;
    _logger = logger;
}
```

Add logging in CreateSession method, after `await _sessionRepository.SaveAsync(session);`:
```csharp
_logger.LogInformation("Session started: {SessionId} for maze {MazeId}",
    session.Id, mazeId);
```

Add logging in GetSession method, before `return NotFound(...)` for session not found:
```csharp
_logger.LogWarning("Session not found: {SessionId}", sessionId);
```

Add logging in Move method, after `await _sessionRepository.SaveAsync(session);`:
```csharp
_logger.LogInformation("Move {Direction}: {Result} for session {SessionId}",
    direction, moveResult, sessionId);

if (session.State == SessionState.Completed)
{
    _logger.LogInformation("Session {SessionId} completed maze {MazeId}",
        sessionId, mazeId);
}
```

**Step 4: Run tests to verify they pass**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: All tests pass (184 tests - 4 new)

**Step 5: Commit**

```bash
git add tests/MazeOfHateoas.UnitTests/Controllers/SessionsControllerLoggingTests.cs src/MazeOfHateoas.Api/Controllers/SessionsController.cs
git commit -m "feat(logging): add logging to SessionsController with tests"
```

---

## Task 7: Verify End-to-End Logging

**Files:**
- None (verification only)

**Step 1: Run the application and verify JSON logging**

Run: `docker compose up --build`

**Step 2: Make a test request**

In another terminal:
```bash
curl -X POST http://localhost:8080/api/mazes -H "Content-Type: application/json" -d '{"width": 5, "height": 5}'
```

**Step 3: Verify JSON log output**

Expected: Console shows JSON structured logs like:
```json
{"@t":"...","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms",...}
{"@t":"...","@mt":"Maze created: {MazeId} ({Width}x{Height})",...}
```

**Step 4: Stop the application**

Run: `docker compose down`

**Step 5: Run all tests one final time**

Run: `docker compose -f docker-compose.test.yml run --rm test`

Expected: All tests pass

---

## Task 8: Update docker-compose with LOG_LEVEL example

**Files:**
- Modify: `docker-compose.yml`

**Step 1: Add Serilog environment variable example**

Add to environment section of the api service (as a comment for documentation):

```yaml
# Log level can be overridden via environment variable:
# - Serilog__MinimumLevel__Default=Warning
```

**Step 2: Commit final changes**

```bash
git add docker-compose.yml
git commit -m "docs: add log level environment variable example to docker-compose"
```

---

## Summary

| Task | Description | Tests Added |
|------|-------------|-------------|
| 1 | Add NuGet packages | 0 |
| 2 | Configure appsettings | 0 |
| 3 | Set up Serilog in Program.cs | 0 |
| 4 | Add exception logging | 0 |
| 5 | Add MazesController logging | 2 |
| 6 | Add SessionsController logging | 4 |
| 7 | End-to-end verification | 0 |
| 8 | Docker-compose documentation | 0 |

**Total new tests:** 6
**Expected final test count:** 184
