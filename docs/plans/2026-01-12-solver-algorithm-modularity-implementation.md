# Solver Algorithm Modularity Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable configurable maze-solving algorithms (DFS, BFS, Random) via appsettings.json with environment variable override.

**Architecture:** Factory pattern in DI registration selects solver implementation at startup. Three solver classes implement `ISolver`. Configuration follows existing 12-factor pattern.

**Tech Stack:** .NET 8, xUnit, Moq, Microsoft.Extensions.Configuration

---

## Task 1: Add Algorithm Property to SolverSettings

**Files:**
- Modify: `src/MazeOfHateoas.Solver/Configuration/SolverSettings.cs`
- Modify: `tests/MazeOfHateoas.Solver.UnitTests/Configuration/SolverSettingsTests.cs`

**Step 1: Write the failing test**

Add to `SolverSettingsTests.cs`:

```csharp
[Fact]
public void Algorithm_DefaultsToDfs()
{
    var settings = new SolverSettings();

    Assert.Equal("dfs", settings.Algorithm);
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~SolverSettingsTests.Algorithm_DefaultsToDfs"`

Expected: FAIL - 'SolverSettings' does not contain a definition for 'Algorithm'

**Step 3: Write minimal implementation**

Add to `SolverSettings.cs`:

```csharp
public string Algorithm { get; set; } = "dfs";
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~SolverSettingsTests.Algorithm_DefaultsToDfs"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Configuration/SolverSettings.cs tests/MazeOfHateoas.Solver.UnitTests/Configuration/SolverSettingsTests.cs
git commit -m "feat(solver): add Algorithm property to SolverSettings"
```

---

## Task 2: Create appsettings.json and Update Program.cs

**Files:**
- Create: `src/MazeOfHateoas.Solver/appsettings.json`
- Modify: `src/MazeOfHateoas.Solver/Program.cs`
- Modify: `src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj`

**Step 1: Create appsettings.json**

Create `src/MazeOfHateoas.Solver/appsettings.json`:

```json
{
  "Solver": {
    "Algorithm": "dfs",
    "ApiBaseUrl": "http://localhost:8080",
    "MazeWidth": 10,
    "MazeHeight": 10,
    "DelayBetweenMazesMs": 2000,
    "DelayBetweenMovesMs": 0,
    "StatsIntervalMazes": 10
  }
}
```

**Step 2: Update csproj to copy appsettings.json**

Add to `MazeOfHateoas.Solver.csproj` inside `<Project>`:

```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**Step 3: Update Program.cs configuration binding**

Replace the `Configure<SolverSettings>` block with:

```csharp
builder.Services.Configure<SolverSettings>(options =>
{
    var section = builder.Configuration.GetSection(SolverSettings.SectionName);
    section.Bind(options);

    // Environment variable overrides
    options.ApiBaseUrl = builder.Configuration["SOLVER_API_BASE_URL"] ?? options.ApiBaseUrl;
    options.MazeWidth = int.TryParse(builder.Configuration["SOLVER_MAZE_WIDTH"], out var w) ? w : options.MazeWidth;
    options.MazeHeight = int.TryParse(builder.Configuration["SOLVER_MAZE_HEIGHT"], out var h) ? h : options.MazeHeight;
    options.DelayBetweenMazesMs = int.TryParse(builder.Configuration["SOLVER_DELAY_BETWEEN_MAZES_MS"], out var dm) ? dm : options.DelayBetweenMazesMs;
    options.DelayBetweenMovesMs = int.TryParse(builder.Configuration["SOLVER_DELAY_BETWEEN_MOVES_MS"], out var dmv) ? dmv : options.DelayBetweenMovesMs;
    options.StatsIntervalMazes = int.TryParse(builder.Configuration["SOLVER_STATS_INTERVAL_MAZES"], out var si) ? si : options.StatsIntervalMazes;
    options.Algorithm = builder.Configuration["SOLVER_ALGORITHM"] ?? options.Algorithm;
});
```

**Step 4: Run existing tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~Solver.UnitTests"`

Expected: All tests PASS

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/appsettings.json src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj src/MazeOfHateoas.Solver/Program.cs
git commit -m "feat(solver): add appsettings.json with env var override"
```

---

## Task 3: Rename HateoasSolver to DepthFirstSolver

**Files:**
- Rename: `src/MazeOfHateoas.Solver/Services/HateoasSolver.cs` → `DepthFirstSolver.cs`
- Rename: `tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs` → `DepthFirstSolverTests.cs`
- Modify: `src/MazeOfHateoas.Solver/Program.cs`

**Step 1: Rename source file and class**

Rename file to `DepthFirstSolver.cs` and update class:

```csharp
public class DepthFirstSolver : ISolver
{
    private readonly ILogger<DepthFirstSolver> _logger;

    public DepthFirstSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<DepthFirstSolver> logger)
    // ... rest unchanged
}
```

**Step 2: Rename test file and class**

Rename file to `DepthFirstSolverTests.cs` and update:

```csharp
public class DepthFirstSolverTests
{
    private readonly Mock<ILogger<DepthFirstSolver>> _mockLogger;

    public DepthFirstSolverTests()
    {
        _mockLogger = new Mock<ILogger<DepthFirstSolver>>();
        // ... rest unchanged
    }

    // Update all `new HateoasSolver(...)` to `new DepthFirstSolver(...)`
}
```

**Step 3: Update Program.cs registration**

Change:
```csharp
builder.Services.AddSingleton<ISolver, HateoasSolver>();
```
To:
```csharp
builder.Services.AddSingleton<ISolver, DepthFirstSolver>();
```

**Step 4: Run tests to verify rename worked**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~DepthFirstSolverTests"`

Expected: All 4 tests PASS

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor(solver): rename HateoasSolver to DepthFirstSolver"
```

---

## Task 4: Create BreadthFirstSolver with Tests (TDD)

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/BreadthFirstSolver.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/BreadthFirstSolverTests.cs`

**Step 1: Write the first failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/BreadthFirstSolverTests.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class BreadthFirstSolverTests
{
    private readonly Mock<IMazeApiClient> _mockApiClient;
    private readonly Mock<ILogger<BreadthFirstSolver>> _mockLogger;
    private readonly IOptions<SolverSettings> _settings;
    private readonly Guid _mazeId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public BreadthFirstSolverTests()
    {
        _mockApiClient = new Mock<IMazeApiClient>();
        _mockLogger = new Mock<ILogger<BreadthFirstSolver>>();
        _settings = Options.Create(new SolverSettings { DelayBetweenMovesMs = 0 });
    }

    [Fact]
    public async Task SolveAsync_WhenAlreadyAtEnd_ReturnsImmediately()
    {
        var maze = CreateMaze();
        var completedSession = CreateSession(9, 9, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

        var solver = new BreadthFirstSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(0, result.MoveCount);
    }

    private MazeResponse CreateMaze() => new()
    {
        Id = _mazeId,
        Width = 10,
        Height = 10,
        Start = new PositionDto { X = 0, Y = 0 },
        End = new PositionDto { X = 9, Y = 9 },
        CreatedAt = DateTime.UtcNow,
        Links = new Dictionary<string, Link>
        {
            ["start"] = new Link { Href = "/api/mazes/123/sessions", Rel = "start", Method = "POST" }
        }
    };

    private SessionResponse CreateSession(int x, int y, string state, params (string name, string href)[] moves) => new()
    {
        Id = _sessionId,
        MazeId = _mazeId,
        CurrentPosition = new PositionDto { X = x, Y = y },
        State = state,
        Links = moves.ToDictionary(
            m => m.name,
            m => new Link { Href = m.href, Rel = "move", Method = "POST" }
        )
    };
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~BreadthFirstSolverTests.SolveAsync_WhenAlreadyAtEnd"`

Expected: FAIL - type 'BreadthFirstSolver' could not be found

**Step 3: Write minimal BreadthFirstSolver implementation**

Create `src/MazeOfHateoas.Solver/Services/BreadthFirstSolver.cs`:

```csharp
using System.Diagnostics;
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class BreadthFirstSolver : ISolver
{
    private static readonly string[] Directions = ["north", "south", "east", "west"];

    private readonly IMazeApiClient _apiClient;
    private readonly SolverSettings _settings;
    private readonly ILogger<BreadthFirstSolver> _logger;

    public BreadthFirstSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<BreadthFirstSolver> logger)
    {
        _apiClient = apiClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var moveCount = 0;

        var startLink = maze.Links["start"];
        var session = await _apiClient.StartSessionAsync(startLink, ct);

        _logger.LogInformation("BFS: Started maze {MazeId}, session {SessionId} at ({X},{Y})",
            maze.Id, session.Id, session.CurrentPosition.X, session.CurrentPosition.Y);

        var visited = new HashSet<(int X, int Y)>();
        var parentMap = new Dictionary<(int X, int Y), (int X, int Y)?>();
        var queue = new Queue<(int X, int Y)>();

        var start = (session.CurrentPosition.X, session.CurrentPosition.Y);
        visited.Add(start);
        parentMap[start] = null;
        queue.Enqueue(start);

        var currentPos = start;
        var targetPath = new List<(int X, int Y)>();

        while (session.State != "Completed" && !ct.IsCancellationRequested)
        {
            // If we have a target path, follow it
            if (targetPath.Count > 0)
            {
                var nextPos = targetPath[0];
                targetPath.RemoveAt(0);

                var availableMoves = GetAvailableMoves(session);
                var direction = GetDirectionTo(currentPos, nextPos);
                var moveLink = availableMoves.FirstOrDefault(m => m.direction == direction).link;

                if (moveLink != null)
                {
                    session = await _apiClient.MoveAsync(moveLink, ct);
                    currentPos = (session.CurrentPosition.X, session.CurrentPosition.Y);
                    moveCount++;

                    _logger.LogDebug("BFS: Moved {Direction} to ({X},{Y})",
                        direction, currentPos.X, currentPos.Y);

                    if (_settings.DelayBetweenMovesMs > 0)
                        await Task.Delay(_settings.DelayBetweenMovesMs, ct);
                }
                continue;
            }

            // BFS exploration: find next unvisited cell
            if (queue.Count > 0)
            {
                var exploring = queue.Dequeue();

                // Navigate to this cell if not already there
                if (exploring != currentPos)
                {
                    targetPath = BuildPath(parentMap, currentPos, exploring);
                    continue;
                }

                // Explore neighbors
                var availableMoves = GetAvailableMoves(session);
                foreach (var (direction, link) in availableMoves)
                {
                    var neighbor = GetTargetPosition(currentPos, direction);
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parentMap[neighbor] = currentPos;
                        queue.Enqueue(neighbor);
                    }
                }

                // Move to first unvisited neighbor
                var unvisitedMove = availableMoves
                    .FirstOrDefault(m => !visited.Contains(GetTargetPosition(currentPos, m.direction)) == false
                        && parentMap.ContainsKey(GetTargetPosition(currentPos, m.direction))
                        && parentMap[GetTargetPosition(currentPos, m.direction)] == currentPos);

                if (unvisitedMove.link != null)
                {
                    session = await _apiClient.MoveAsync(unvisitedMove.link, ct);
                    currentPos = (session.CurrentPosition.X, session.CurrentPosition.Y);
                    moveCount++;

                    if (_settings.DelayBetweenMovesMs > 0)
                        await Task.Delay(_settings.DelayBetweenMovesMs, ct);
                }
            }
            else
            {
                _logger.LogWarning("BFS: Queue empty but maze not completed at ({X},{Y})",
                    currentPos.X, currentPos.Y);
                break;
            }
        }

        stopwatch.Stop();
        var success = session.State == "Completed";

        _logger.LogInformation("BFS: Maze {MazeId} {Result} in {MoveCount} moves ({ElapsedMs}ms)",
            maze.Id, success ? "solved" : "failed", moveCount, stopwatch.ElapsedMilliseconds);

        return new SolveResult(maze.Id, session.Id, moveCount, stopwatch.ElapsedMilliseconds, success);
    }

    private static List<(int X, int Y)> BuildPath(
        Dictionary<(int X, int Y), (int X, int Y)?> parentMap,
        (int X, int Y) from,
        (int X, int Y) to)
    {
        // Find common ancestor and build path
        var fromAncestors = new HashSet<(int X, int Y)>();
        var current = from;
        while (parentMap.ContainsKey(current))
        {
            fromAncestors.Add(current);
            if (parentMap[current] == null) break;
            current = parentMap[current]!.Value;
        }

        var toPath = new List<(int X, int Y)>();
        current = to;
        while (!fromAncestors.Contains(current) && parentMap.ContainsKey(current))
        {
            toPath.Add(current);
            if (parentMap[current] == null) break;
            current = parentMap[current]!.Value;
        }

        var commonAncestor = current;

        var fromPath = new List<(int X, int Y)>();
        current = from;
        while (current != commonAncestor && parentMap.ContainsKey(current))
        {
            if (parentMap[current] == null) break;
            current = parentMap[current]!.Value;
            fromPath.Add(current);
        }

        toPath.Reverse();
        fromPath.AddRange(toPath);
        return fromPath;
    }

    private static string GetDirectionTo((int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        if (dx > 0) return "east";
        if (dx < 0) return "west";
        if (dy > 0) return "south";
        return "north";
    }

    private static List<(string direction, Link link)> GetAvailableMoves(SessionResponse session) =>
        Directions
            .Where(d => session.Links.ContainsKey(d))
            .Select(d => (d, session.Links[d]))
            .ToList();

    private static (int X, int Y) GetTargetPosition((int X, int Y) from, string direction) => direction switch
    {
        "north" => (from.X, from.Y - 1),
        "south" => (from.X, from.Y + 1),
        "east" => (from.X + 1, from.Y),
        "west" => (from.X - 1, from.Y),
        _ => from
    };
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~BreadthFirstSolverTests.SolveAsync_WhenAlreadyAtEnd"`

Expected: PASS

**Step 5: Add remaining BFS tests**

Add to `BreadthFirstSolverTests.cs`:

```csharp
[Fact]
public async Task SolveAsync_WhenOneMove_CompletesInOneMove()
{
    var maze = CreateMaze();
    var startSession = CreateSession(0, 0, "InProgress", ("east", "/move/east"));
    var endSession = CreateSession(1, 0, "Completed");

    _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(startSession);
    _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(endSession);

    var solver = new BreadthFirstSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

    var result = await solver.SolveAsync(maze);

    Assert.True(result.Success);
    Assert.Equal(1, result.MoveCount);
}

[Fact]
public async Task SolveAsync_WhenDeadEnd_BacktracksToLastJunction()
{
    var maze = CreateMaze();

    var moveSequence = new Queue<SessionResponse>(new[]
    {
        CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
        CreateSession(1, 0, "InProgress", ("west", "/move/west")),
        CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
        CreateSession(0, 1, "Completed")
    });

    _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(moveSequence.Dequeue());
    _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(() => moveSequence.Dequeue());

    var solver = new BreadthFirstSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

    var result = await solver.SolveAsync(maze);

    Assert.True(result.Success);
}
```

**Step 6: Run all BFS tests**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~BreadthFirstSolverTests"`

Expected: All tests PASS

**Step 7: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/BreadthFirstSolver.cs tests/MazeOfHateoas.Solver.UnitTests/Services/BreadthFirstSolverTests.cs
git commit -m "feat(solver): add BreadthFirstSolver implementation"
```

---

## Task 5: Create RandomWalkSolver with Tests (TDD)

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/RandomWalkSolver.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/RandomWalkSolverTests.cs`

**Step 1: Write the first failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/RandomWalkSolverTests.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class RandomWalkSolverTests
{
    private readonly Mock<IMazeApiClient> _mockApiClient;
    private readonly Mock<ILogger<RandomWalkSolver>> _mockLogger;
    private readonly IOptions<SolverSettings> _settings;
    private readonly Guid _mazeId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public RandomWalkSolverTests()
    {
        _mockApiClient = new Mock<IMazeApiClient>();
        _mockLogger = new Mock<ILogger<RandomWalkSolver>>();
        _settings = Options.Create(new SolverSettings { DelayBetweenMovesMs = 0 });
    }

    [Fact]
    public async Task SolveAsync_WhenAlreadyAtEnd_ReturnsImmediately()
    {
        var maze = CreateMaze();
        var completedSession = CreateSession(9, 9, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

        var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(0, result.MoveCount);
    }

    private MazeResponse CreateMaze() => new()
    {
        Id = _mazeId,
        Width = 10,
        Height = 10,
        Start = new PositionDto { X = 0, Y = 0 },
        End = new PositionDto { X = 9, Y = 9 },
        CreatedAt = DateTime.UtcNow,
        Links = new Dictionary<string, Link>
        {
            ["start"] = new Link { Href = "/api/mazes/123/sessions", Rel = "start", Method = "POST" }
        }
    };

    private SessionResponse CreateSession(int x, int y, string state, params (string name, string href)[] moves) => new()
    {
        Id = _sessionId,
        MazeId = _mazeId,
        CurrentPosition = new PositionDto { X = x, Y = y },
        State = state,
        Links = moves.ToDictionary(
            m => m.name,
            m => new Link { Href = m.href, Rel = "move", Method = "POST" }
        )
    };
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~RandomWalkSolverTests.SolveAsync_WhenAlreadyAtEnd"`

Expected: FAIL - type 'RandomWalkSolver' could not be found

**Step 3: Write minimal RandomWalkSolver implementation**

Create `src/MazeOfHateoas.Solver/Services/RandomWalkSolver.cs`:

```csharp
using System.Diagnostics;
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class RandomWalkSolver : ISolver
{
    private static readonly string[] Directions = ["north", "south", "east", "west"];

    private readonly IMazeApiClient _apiClient;
    private readonly SolverSettings _settings;
    private readonly ILogger<RandomWalkSolver> _logger;
    private readonly Random _random;

    public RandomWalkSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<RandomWalkSolver> logger)
        : this(apiClient, settings, logger, new Random())
    {
    }

    internal RandomWalkSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<RandomWalkSolver> logger,
        Random random)
    {
        _apiClient = apiClient;
        _settings = settings.Value;
        _logger = logger;
        _random = random;
    }

    public async Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var moveCount = 0;

        var startLink = maze.Links["start"];
        var session = await _apiClient.StartSessionAsync(startLink, ct);

        _logger.LogInformation("Random: Started maze {MazeId}, session {SessionId} at ({X},{Y})",
            maze.Id, session.Id, session.CurrentPosition.X, session.CurrentPosition.Y);

        var visited = new HashSet<(int X, int Y)>();
        var backtrackStack = new Stack<(int X, int Y)>();
        visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));

        while (session.State != "Completed" && !ct.IsCancellationRequested)
        {
            var currentPos = (session.CurrentPosition.X, session.CurrentPosition.Y);
            var availableMoves = GetAvailableMoves(session);
            var unvisitedMoves = availableMoves
                .Where(m => !visited.Contains(GetTargetPosition(currentPos, m.direction)))
                .ToList();

            Link? moveLink;
            string direction;

            if (unvisitedMoves.Count > 0)
            {
                backtrackStack.Push(currentPos);
                // Random selection among unvisited
                var randomIndex = _random.Next(unvisitedMoves.Count);
                (direction, moveLink) = unvisitedMoves[randomIndex];
            }
            else if (backtrackStack.Count > 0)
            {
                var backtrackTo = backtrackStack.Pop();
                (direction, moveLink) = GetMoveToward(currentPos, backtrackTo, availableMoves);
            }
            else
            {
                _logger.LogWarning("Random: No moves available and backtrack stack empty at ({X},{Y})",
                    currentPos.X, currentPos.Y);
                break;
            }

            var targetPos = GetTargetPosition(currentPos, direction);
            _logger.LogDebug("Random: Moving {Direction} from ({FromX},{FromY}) to ({ToX},{ToY}), visited: {VisitedCount}",
                direction, currentPos.X, currentPos.Y, targetPos.X, targetPos.Y, visited.Count);

            session = await _apiClient.MoveAsync(moveLink, ct);
            visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));
            moveCount++;

            if (_settings.DelayBetweenMovesMs > 0)
                await Task.Delay(_settings.DelayBetweenMovesMs, ct);
        }

        stopwatch.Stop();
        var success = session.State == "Completed";

        _logger.LogInformation("Random: Maze {MazeId} {Result} in {MoveCount} moves ({ElapsedMs}ms)",
            maze.Id, success ? "solved" : "failed", moveCount, stopwatch.ElapsedMilliseconds);

        return new SolveResult(maze.Id, session.Id, moveCount, stopwatch.ElapsedMilliseconds, success);
    }

    private static List<(string direction, Link link)> GetAvailableMoves(SessionResponse session) =>
        Directions
            .Where(d => session.Links.ContainsKey(d))
            .Select(d => (d, session.Links[d]))
            .ToList();

    private static (int X, int Y) GetTargetPosition((int X, int Y) from, string direction) => direction switch
    {
        "north" => (from.X, from.Y - 1),
        "south" => (from.X, from.Y + 1),
        "east" => (from.X + 1, from.Y),
        "west" => (from.X - 1, from.Y),
        _ => from
    };

    private static (string direction, Link link) GetMoveToward(
        (int X, int Y) from,
        (int X, int Y) target,
        List<(string direction, Link link)> availableMoves)
    {
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;

        string preferredDirection;
        if (dx > 0) preferredDirection = "east";
        else if (dx < 0) preferredDirection = "west";
        else if (dy > 0) preferredDirection = "south";
        else preferredDirection = "north";

        return availableMoves.FirstOrDefault(m => m.direction == preferredDirection);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~RandomWalkSolverTests.SolveAsync_WhenAlreadyAtEnd"`

Expected: PASS

**Step 5: Add remaining RandomWalk tests**

Add to `RandomWalkSolverTests.cs`:

```csharp
[Fact]
public async Task SolveAsync_WhenOneMove_CompletesInOneMove()
{
    var maze = CreateMaze();
    var startSession = CreateSession(0, 0, "InProgress", ("east", "/move/east"));
    var endSession = CreateSession(1, 0, "Completed");

    _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(startSession);
    _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(endSession);

    var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

    var result = await solver.SolveAsync(maze);

    Assert.True(result.Success);
    Assert.Equal(1, result.MoveCount);
}

[Fact]
public async Task SolveAsync_WhenDeadEnd_BacktracksToLastJunction()
{
    var maze = CreateMaze();

    var moveSequence = new Queue<SessionResponse>(new[]
    {
        CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
        CreateSession(1, 0, "InProgress", ("west", "/move/west")),
        CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
        CreateSession(0, 1, "Completed")
    });

    _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(moveSequence.Dequeue());
    _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(() => moveSequence.Dequeue());

    var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

    var result = await solver.SolveAsync(maze);

    Assert.True(result.Success);
    Assert.Equal(3, result.MoveCount);
}

[Fact]
public async Task SolveAsync_WithSeededRandom_ChoosesRandomlyAmongUnvisited()
{
    var maze = CreateMaze();
    var seededRandom = new Random(42); // Deterministic for testing

    var chosenDirections = new List<string>();
    var moveSequence = new Queue<SessionResponse>(new[]
    {
        CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
        CreateSession(0, 1, "Completed") // or (1,0) depending on random choice
    });

    _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(moveSequence.Dequeue());
    _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
        .Callback<Link, CancellationToken>((link, _) => chosenDirections.Add(link.Href))
        .ReturnsAsync(() => moveSequence.Dequeue());

    var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object, seededRandom);

    await solver.SolveAsync(maze);

    // With seed 42, it should make a deterministic choice
    Assert.Single(chosenDirections);
}
```

**Step 6: Run all RandomWalk tests**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~RandomWalkSolverTests"`

Expected: All tests PASS

**Step 7: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/RandomWalkSolver.cs tests/MazeOfHateoas.Solver.UnitTests/Services/RandomWalkSolverTests.cs
git commit -m "feat(solver): add RandomWalkSolver implementation"
```

---

## Task 6: Add Factory Registration in Program.cs

**Files:**
- Modify: `src/MazeOfHateoas.Solver/Program.cs`

**Step 1: Update ISolver registration to factory pattern**

Replace:
```csharp
builder.Services.AddSingleton<ISolver, DepthFirstSolver>();
```

With:
```csharp
builder.Services.AddSingleton<ISolver>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<SolverSettings>>().Value;
    var apiClient = sp.GetRequiredService<IMazeApiClient>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var algorithm = settings.Algorithm.ToLowerInvariant();
    Log.Information("Using solver algorithm: {Algorithm}", algorithm);

    return algorithm switch
    {
        "bfs" => new BreadthFirstSolver(
            apiClient,
            sp.GetRequiredService<IOptions<SolverSettings>>(),
            loggerFactory.CreateLogger<BreadthFirstSolver>()),
        "random" => new RandomWalkSolver(
            apiClient,
            sp.GetRequiredService<IOptions<SolverSettings>>(),
            loggerFactory.CreateLogger<RandomWalkSolver>()),
        _ => new DepthFirstSolver(
            apiClient,
            sp.GetRequiredService<IOptions<SolverSettings>>(),
            loggerFactory.CreateLogger<DepthFirstSolver>())
    };
});
```

**Step 2: Run all solver tests to verify nothing broke**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~Solver.UnitTests"`

Expected: All tests PASS

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Solver/Program.cs
git commit -m "feat(solver): add factory registration for algorithm selection"
```

---

## Task 7: Update docker-compose.solver.yml

**Files:**
- Modify: `docker-compose.solver.yml`

**Step 1: Add SOLVER_ALGORITHM environment variable**

Add to solver service environment:

```yaml
- SOLVER_ALGORITHM=dfs
```

**Step 2: Commit**

```bash
git add docker-compose.solver.yml
git commit -m "feat(solver): add SOLVER_ALGORITHM to docker-compose"
```

---

## Task 8: Final Integration Test

**Step 1: Run all tests**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test`

Expected: All tests PASS

**Step 2: Manual smoke test with each algorithm**

Test DFS:
```bash
SOLVER_ALGORITHM=dfs docker compose -f docker-compose.solver.yml up --build
```

Test BFS:
```bash
SOLVER_ALGORITHM=bfs docker compose -f docker-compose.solver.yml up --build
```

Test Random:
```bash
SOLVER_ALGORITHM=random docker compose -f docker-compose.solver.yml up --build
```

Verify logs show correct algorithm being used.

**Step 3: Final commit if any cleanup needed**

```bash
git add -A
git commit -m "chore(solver): final cleanup for algorithm modularity"
```
