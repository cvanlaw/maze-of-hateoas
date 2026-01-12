# Maze Dashboard UI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a Vue 3 SPA dashboard with real-time visualization of maze API clients via SignalR.

**Architecture:** Extend .NET API with SignalR hub and metrics service. Create standalone Vue 3 app that connects via WebSocket for real-time updates and REST for initial data fetch.

**Tech Stack:** .NET 8, SignalR, Vue 3, TypeScript, Tailwind CSS, Vite

---

## Phase 1: Domain Model Changes

### Task 1.1: Add MoveCount property to MazeSession

**Files:**
- Modify: `src/MazeOfHateoas.Domain/MazeSession.cs`
- Test: `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionTests.cs`:

```csharp
[Fact]
public void MazeSession_MoveCount_InitializedToZero()
{
    var session = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));

    Assert.Equal(0, session.MoveCount);
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeSession_MoveCount_InitializedToZero"
```

Expected: FAIL - 'MazeSession' does not contain a definition for 'MoveCount'

**Step 3: Write minimal implementation**

Add to `src/MazeOfHateoas.Domain/MazeSession.cs` after `StartedAt` property:

```csharp
public int MoveCount { get; private set; }
```

**Step 4: Run test to verify it passes**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeSession_MoveCount_InitializedToZero"
```

Expected: PASS

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(domain): add MoveCount property to MazeSession"
```

---

### Task 1.2: Add VisitedCells property to MazeSession

**Files:**
- Modify: `src/MazeOfHateoas.Domain/MazeSession.cs`
- Test: `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionTests.cs`:

```csharp
[Fact]
public void MazeSession_VisitedCells_ContainsStartPosition()
{
    var startPosition = new Position(0, 0);
    var session = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), startPosition);

    Assert.Contains(startPosition, session.VisitedCells);
    Assert.Single(session.VisitedCells);
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeSession_VisitedCells_ContainsStartPosition"
```

Expected: FAIL - 'MazeSession' does not contain a definition for 'VisitedCells'

**Step 3: Write minimal implementation**

Add to `src/MazeOfHateoas.Domain/MazeSession.cs`:

After `MoveCount` property:
```csharp
public HashSet<Position> VisitedCells { get; } = new();
```

In constructor, after setting `StartedAt`:
```csharp
VisitedCells.Add(startPosition);
```

**Step 4: Run test to verify it passes**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MazeSession_VisitedCells_ContainsStartPosition"
```

Expected: PASS

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(domain): add VisitedCells property to MazeSession"
```

---

### Task 1.3: Increment MoveCount on successful move

**Files:**
- Modify: `src/MazeOfHateoas.Domain/MazeSession.cs`
- Test: `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionMoveTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionMoveTests.cs`:

```csharp
[Fact]
public void Move_Success_IncrementsMoveCount()
{
    var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
    var session = TestDataBuilders.CreateTestSession(maze.Id);

    session.Move(Direction.East, maze);

    Assert.Equal(1, session.MoveCount);
}

[Fact]
public void Move_Blocked_DoesNotIncrementMoveCount()
{
    var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: false);
    var session = TestDataBuilders.CreateTestSession(maze.Id);

    session.Move(Direction.East, maze);

    Assert.Equal(0, session.MoveCount);
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~Move_Success_IncrementsMoveCount"
```

Expected: FAIL - Assert.Equal() Failure: Expected 1, Actual 0

**Step 3: Write minimal implementation**

In `src/MazeOfHateoas.Domain/MazeSession.cs`, modify `Move()` method. After `CurrentPosition = newPosition;` add:

```csharp
MoveCount++;
```

**Step 4: Run tests to verify they pass**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MoveCount"
```

Expected: All MoveCount tests PASS

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(domain): increment MoveCount on successful move"
```

---

### Task 1.4: Add new position to VisitedCells on successful move

**Files:**
- Modify: `src/MazeOfHateoas.Domain/MazeSession.cs`
- Test: `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionMoveTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Domain/MazeSessionMoveTests.cs`:

```csharp
[Fact]
public void Move_Success_AddsNewPositionToVisitedCells()
{
    var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
    var session = TestDataBuilders.CreateTestSession(maze.Id);

    session.Move(Direction.East, maze);

    Assert.Contains(new Position(0, 0), session.VisitedCells);
    Assert.Contains(new Position(1, 0), session.VisitedCells);
    Assert.Equal(2, session.VisitedCells.Count);
}

[Fact]
public void Move_ToAlreadyVisitedCell_DoesNotDuplicateInVisitedCells()
{
    var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
    var session = TestDataBuilders.CreateTestSession(maze.Id);

    session.Move(Direction.East, maze);
    session.Move(Direction.West, maze);

    Assert.Equal(2, session.VisitedCells.Count);
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~AddsNewPositionToVisitedCells"
```

Expected: FAIL - Assert.Equal() Failure: Expected 2, Actual 1

**Step 3: Write minimal implementation**

In `src/MazeOfHateoas.Domain/MazeSession.cs`, in `Move()` method after `MoveCount++;` add:

```csharp
VisitedCells.Add(CurrentPosition);
```

**Step 4: Run tests to verify they pass**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~VisitedCells"
```

Expected: All VisitedCells tests PASS

**Step 5: Run all tests to ensure no regressions**

```bash
docker compose -f docker-compose.test.yml up --build
```

Expected: All 190+ tests PASS

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(domain): track visited cells on successful move"
```

---

## Phase 2: Application Layer - Metrics Interfaces

### Task 2.1: Create metrics DTOs

**Files:**
- Create: `src/MazeOfHateoas.Application/DTOs/AggregateMetrics.cs`
- Create: `src/MazeOfHateoas.Application/DTOs/MazeMetrics.cs`
- Create: `src/MazeOfHateoas.Application/DTOs/SessionSnapshot.cs`

**Step 1: Create AggregateMetrics record**

Create file `src/MazeOfHateoas.Application/DTOs/AggregateMetrics.cs`:

```csharp
namespace MazeOfHateoas.Application.DTOs;

public record AggregateMetrics(
    int ActiveSessions,
    int CompletedToday,
    double CompletionRate,
    double AverageMoves,
    Guid? MostActiveMazeId,
    int MostActiveMazeSessionCount,
    double SystemVelocity
);
```

**Step 2: Create SessionSnapshot record**

Create file `src/MazeOfHateoas.Application/DTOs/SessionSnapshot.cs`:

```csharp
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.DTOs;

public record SessionSnapshot(
    Guid SessionId,
    Position CurrentPosition,
    int MoveCount,
    int VisitedCount,
    double CompletionPercent,
    double Velocity,
    TimeSpan Duration
);
```

**Step 3: Create MazeMetrics record**

Create file `src/MazeOfHateoas.Application/DTOs/MazeMetrics.cs`:

```csharp
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.DTOs;

public record MazeMetrics(
    Guid MazeId,
    int Width,
    int Height,
    Cell[,] Cells,
    int ActiveSessions,
    int TotalCompleted,
    List<SessionSnapshot> Sessions
);
```

**Step 4: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(application): add metrics DTOs"
```

---

### Task 2.2: Create IMetricsService interface

**Files:**
- Create: `src/MazeOfHateoas.Application/Interfaces/IMetricsService.cs`

**Step 1: Create the interface**

Create file `src/MazeOfHateoas.Application/Interfaces/IMetricsService.cs`:

```csharp
using MazeOfHateoas.Application.DTOs;

namespace MazeOfHateoas.Application.Interfaces;

public interface IMetricsService
{
    Task<AggregateMetrics> GetAggregateMetricsAsync();
    Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId);
}
```

**Step 2: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(application): add IMetricsService interface"
```

---

### Task 2.3: Extend ISessionRepository with GetAllAsync

**Files:**
- Modify: `src/MazeOfHateoas.Application/Interfaces/ISessionRepository.cs`
- Modify: `src/MazeOfHateoas.Infrastructure/InMemorySessionRepository.cs`
- Test: `tests/MazeOfHateoas.UnitTests/Infrastructure/InMemorySessionRepositoryTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Infrastructure/InMemorySessionRepositoryTests.cs`:

```csharp
[Fact]
public async Task GetAllAsync_ReturnsAllSessions()
{
    var repository = new InMemorySessionRepository();
    var session1 = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));
    var session2 = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));
    await repository.SaveAsync(session1);
    await repository.SaveAsync(session2);

    var all = await repository.GetAllAsync();

    Assert.Equal(2, all.Count());
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~GetAllAsync_ReturnsAllSessions"
```

Expected: FAIL - 'ISessionRepository' does not contain a definition for 'GetAllAsync'

**Step 3: Add to interface**

Add to `src/MazeOfHateoas.Application/Interfaces/ISessionRepository.cs`:

```csharp
Task<IEnumerable<MazeSession>> GetAllAsync();
```

**Step 4: Implement in repository**

Add to `src/MazeOfHateoas.Infrastructure/InMemorySessionRepository.cs`:

```csharp
public Task<IEnumerable<MazeSession>> GetAllAsync()
{
    return Task.FromResult<IEnumerable<MazeSession>>(_sessions.Values.ToList());
}
```

**Step 5: Run test to verify it passes**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~GetAllAsync_ReturnsAllSessions"
```

Expected: PASS

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(infrastructure): add GetAllAsync to ISessionRepository"
```

---

## Phase 3: Infrastructure - MetricsService Implementation

### Task 3.1: Create MetricsService with GetAggregateMetricsAsync

**Files:**
- Create: `src/MazeOfHateoas.Infrastructure/MetricsService.cs`
- Create: `tests/MazeOfHateoas.UnitTests/Infrastructure/MetricsServiceTests.cs`

**Step 1: Write the failing test**

Create file `tests/MazeOfHateoas.UnitTests/Infrastructure/MetricsServiceTests.cs`:

```csharp
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using MazeOfHateoas.Infrastructure;
using MazeOfHateoas.UnitTests.Helpers;

namespace MazeOfHateoas.UnitTests.Infrastructure;

public class MetricsServiceTests
{
    [Fact]
    public async Task GetAggregateMetricsAsync_WithNoSessions_ReturnsZeroCounts()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(0, metrics.ActiveSessions);
        Assert.Equal(0, metrics.CompletedToday);
        Assert.Equal(0, metrics.CompletionRate);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_CountsActiveSessions()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze = TestDataBuilders.CreateTestMaze();
        var session1 = TestDataBuilders.CreateTestSession(maze.Id);
        var session2 = TestDataBuilders.CreateTestSession(maze.Id);
        await mazeRepo.SaveAsync(maze);
        await sessionRepo.SaveAsync(session1);
        await sessionRepo.SaveAsync(session2);
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(2, metrics.ActiveSessions);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MetricsServiceTests"
```

Expected: FAIL - type 'MetricsService' could not be found

**Step 3: Create minimal implementation**

Create file `src/MazeOfHateoas.Infrastructure/MetricsService.cs`:

```csharp
using MazeOfHateoas.Application.DTOs;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Infrastructure;

public class MetricsService : IMetricsService
{
    private readonly IMazeRepository _mazeRepository;
    private readonly ISessionRepository _sessionRepository;

    public MetricsService(IMazeRepository mazeRepository, ISessionRepository sessionRepository)
    {
        _mazeRepository = mazeRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AggregateMetrics> GetAggregateMetricsAsync()
    {
        var sessions = (await _sessionRepository.GetAllAsync()).ToList();
        var activeSessions = sessions.Count(s => s.State == SessionState.InProgress);
        var completedToday = sessions.Count(s =>
            s.State == SessionState.Completed &&
            s.StartedAt.Date == DateTime.UtcNow.Date);
        var totalCompleted = sessions.Count(s => s.State == SessionState.Completed);
        var completionRate = sessions.Count > 0
            ? (double)totalCompleted / sessions.Count * 100
            : 0;
        var averageMoves = totalCompleted > 0
            ? sessions.Where(s => s.State == SessionState.Completed).Average(s => s.MoveCount)
            : 0;

        var mostActiveMaze = sessions
            .Where(s => s.State == SessionState.InProgress)
            .GroupBy(s => s.MazeId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var systemVelocity = sessions
            .Where(s => s.State == SessionState.InProgress)
            .Sum(s => CalculateVelocity(s));

        return new AggregateMetrics(
            activeSessions,
            completedToday,
            Math.Round(completionRate, 1),
            Math.Round(averageMoves, 1),
            mostActiveMaze?.Key,
            mostActiveMaze?.Count() ?? 0,
            Math.Round(systemVelocity, 1)
        );
    }

    public Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId)
    {
        throw new NotImplementedException();
    }

    private static double CalculateVelocity(MazeSession session)
    {
        var duration = DateTime.UtcNow - session.StartedAt;
        return duration.TotalMinutes > 0
            ? session.MoveCount / duration.TotalMinutes
            : 0;
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MetricsServiceTests"
```

Expected: PASS

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(infrastructure): implement MetricsService.GetAggregateMetricsAsync"
```

---

### Task 3.2: Implement GetMazeMetricsAsync

**Files:**
- Modify: `src/MazeOfHateoas.Infrastructure/MetricsService.cs`
- Modify: `tests/MazeOfHateoas.UnitTests/Infrastructure/MetricsServiceTests.cs`

**Step 1: Write the failing test**

Add to `tests/MazeOfHateoas.UnitTests/Infrastructure/MetricsServiceTests.cs`:

```csharp
[Fact]
public async Task GetMazeMetricsAsync_WithValidMaze_ReturnsMetrics()
{
    var mazeRepo = new TestMazeRepository();
    var sessionRepo = new TestSessionRepository();
    var maze = TestDataBuilders.CreateTestMaze(5, 5);
    var session = TestDataBuilders.CreateTestSession(maze.Id);
    await mazeRepo.SaveAsync(maze);
    await sessionRepo.SaveAsync(session);
    var service = new MetricsService(mazeRepo, sessionRepo);

    var metrics = await service.GetMazeMetricsAsync(maze.Id);

    Assert.NotNull(metrics);
    Assert.Equal(maze.Id, metrics.MazeId);
    Assert.Equal(5, metrics.Width);
    Assert.Equal(5, metrics.Height);
    Assert.Equal(1, metrics.ActiveSessions);
    Assert.Single(metrics.Sessions);
}

[Fact]
public async Task GetMazeMetricsAsync_WithInvalidMaze_ReturnsNull()
{
    var mazeRepo = new TestMazeRepository();
    var sessionRepo = new TestSessionRepository();
    var service = new MetricsService(mazeRepo, sessionRepo);

    var metrics = await service.GetMazeMetricsAsync(Guid.NewGuid());

    Assert.Null(metrics);
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~GetMazeMetricsAsync"
```

Expected: FAIL - NotImplementedException

**Step 3: Implement GetMazeMetricsAsync**

Replace `GetMazeMetricsAsync` in `src/MazeOfHateoas.Infrastructure/MetricsService.cs`:

```csharp
public async Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId)
{
    var maze = await _mazeRepository.GetByIdAsync(mazeId);
    if (maze == null) return null;

    var sessions = (await _sessionRepository.GetByMazeIdAsync(mazeId)).ToList();
    var activeSessions = sessions.Where(s => s.State == SessionState.InProgress).ToList();
    var completedCount = sessions.Count(s => s.State == SessionState.Completed);

    var totalCells = maze.Width * maze.Height;

    var snapshots = activeSessions.Select(s => new SessionSnapshot(
        s.Id,
        s.CurrentPosition,
        s.MoveCount,
        s.VisitedCells.Count,
        Math.Round((double)s.VisitedCells.Count / totalCells * 100, 1),
        Math.Round(CalculateVelocity(s), 1),
        DateTime.UtcNow - s.StartedAt
    )).ToList();

    return new MazeMetrics(
        maze.Id,
        maze.Width,
        maze.Height,
        maze.Cells,
        activeSessions.Count,
        completedCount,
        snapshots
    );
}
```

**Step 4: Run tests to verify they pass**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~GetMazeMetricsAsync"
```

Expected: PASS

**Step 5: Run all tests**

```bash
docker compose -f docker-compose.test.yml up --build
```

Expected: All tests PASS

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(infrastructure): implement MetricsService.GetMazeMetricsAsync"
```

---

## Phase 4: SignalR Hub

### Task 4.1: Add SignalR package

**Files:**
- Modify: `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj`

**Step 1: Add package reference**

Add to `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj` in the `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
```

Note: SignalR is included in ASP.NET Core, but we need the types for the hub. Actually, for .NET 8, SignalR is built-in. Skip package addition if build succeeds without it.

**Step 2: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 3: Commit (if package was added)**

```bash
git add -A && git commit -m "chore(api): add SignalR package reference"
```

---

### Task 4.2: Create MetricsHub

**Files:**
- Create: `src/MazeOfHateoas.Api/Hubs/MetricsHub.cs`

**Step 1: Create the hub**

Create file `src/MazeOfHateoas.Api/Hubs/MetricsHub.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;

namespace MazeOfHateoas.Api.Hubs;

public class MetricsHub : Hub
{
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
    }

    public async Task SubscribeToMaze(Guid mazeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"maze:{mazeId}");
    }

    public async Task Unsubscribe()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");
    }

    public async Task UnsubscribeFromMaze(Guid mazeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"maze:{mazeId}");
    }
}
```

**Step 2: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(api): create MetricsHub for real-time updates"
```

---

### Task 4.3: Create hub event DTOs

**Files:**
- Create: `src/MazeOfHateoas.Api/Hubs/HubEvents.cs`

**Step 1: Create event records**

Create file `src/MazeOfHateoas.Api/Hubs/HubEvents.cs`:

```csharp
namespace MazeOfHateoas.Api.Hubs;

public record SessionStartedEvent(
    Guid SessionId,
    Guid MazeId,
    DateTime Timestamp
);

public record SessionMovedEvent(
    Guid SessionId,
    Guid MazeId,
    int PositionX,
    int PositionY,
    int MoveCount,
    int VisitedCount
);

public record SessionCompletedEvent(
    Guid SessionId,
    Guid MazeId,
    int MoveCount,
    TimeSpan Duration
);
```

**Step 2: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(api): add SignalR hub event DTOs"
```

---

### Task 4.4: Register SignalR and configure hub endpoint

**Files:**
- Modify: `src/MazeOfHateoas.Api/Program.cs`

**Step 1: Add SignalR services and endpoint**

In `src/MazeOfHateoas.Api/Program.cs`:

After `builder.Services.AddControllers();` add:
```csharp
builder.Services.AddSignalR();
```

After `app.MapControllers();` add:
```csharp
app.MapHub<MetricsHub>("/hubs/metrics");
```

Add using at top of file:
```csharp
using MazeOfHateoas.Api.Hubs;
```

**Step 2: Verify build succeeds**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet build
```

Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(api): register SignalR and map MetricsHub endpoint"
```

---

### Task 4.5: Broadcast events from SessionsController

**Files:**
- Modify: `src/MazeOfHateoas.Api/Controllers/SessionsController.cs`

**Step 1: Inject IHubContext and broadcast on CreateSession**

Add to constructor parameters:
```csharp
IHubContext<MetricsHub> metricsHub
```

Add field:
```csharp
private readonly IHubContext<MetricsHub> _metricsHub;
```

Assign in constructor:
```csharp
_metricsHub = metricsHub;
```

Add using:
```csharp
using MazeOfHateoas.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
```

In `CreateSession`, after `await _sessionRepository.SaveAsync(session);` add:
```csharp
var startedEvent = new SessionStartedEvent(session.Id, mazeId, session.StartedAt);
await _metricsHub.Clients.Group("all").SendAsync("SessionStarted", startedEvent);
await _metricsHub.Clients.Group($"maze:{mazeId}").SendAsync("SessionStarted", startedEvent);
```

**Step 2: Broadcast on Move**

In `Move`, after `await _sessionRepository.SaveAsync(session);` add:
```csharp
if (session.State == SessionState.Completed)
{
    var completedEvent = new SessionCompletedEvent(
        session.Id,
        mazeId,
        session.MoveCount,
        DateTime.UtcNow - session.StartedAt);
    await _metricsHub.Clients.Group("all").SendAsync("SessionCompleted", completedEvent);
    await _metricsHub.Clients.Group($"maze:{mazeId}").SendAsync("SessionCompleted", completedEvent);
}
else
{
    var movedEvent = new SessionMovedEvent(
        session.Id,
        mazeId,
        session.CurrentPosition.X,
        session.CurrentPosition.Y,
        session.MoveCount,
        session.VisitedCells.Count);
    await _metricsHub.Clients.Group("all").SendAsync("SessionMoved", movedEvent);
    await _metricsHub.Clients.Group($"maze:{mazeId}").SendAsync("SessionMoved", movedEvent);
}
```

**Step 3: Run all tests**

```bash
docker compose -f docker-compose.test.yml up --build
```

Expected: All tests PASS (some controller tests may need mock updates)

**Step 4: Commit**

```bash
git add -A && git commit -m "feat(api): broadcast SignalR events from SessionsController"
```

---

## Phase 5: API Layer - MetricsController

### Task 5.1: Create MetricsController

**Files:**
- Create: `src/MazeOfHateoas.Api/Controllers/MetricsController.cs`
- Create: `tests/MazeOfHateoas.IntegrationTests/MetricsEndpointTests.cs`

**Step 1: Write the failing integration test**

Create file `tests/MazeOfHateoas.IntegrationTests/MetricsEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MazeOfHateoas.IntegrationTests;

public class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MetricsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAggregateMetrics_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAggregateMetrics_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/api/metrics");
        var content = await response.Content.ReadFromJsonAsync<AggregateMetricsResponse>();

        Assert.NotNull(content);
        Assert.True(content.ActiveSessions >= 0);
    }

    private record AggregateMetricsResponse(
        int ActiveSessions,
        int CompletedToday,
        double CompletionRate,
        double AverageMoves,
        Guid? MostActiveMazeId,
        int MostActiveMazeSessionCount,
        double SystemVelocity
    );
}
```

**Step 2: Run test to verify it fails**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MetricsEndpointTests"
```

Expected: FAIL - 404 Not Found

**Step 3: Create MetricsController**

Create file `src/MazeOfHateoas.Api/Controllers/MetricsController.cs`:

```csharp
using MazeOfHateoas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MazeOfHateoas.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;

    public MetricsController(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAggregateMetrics()
    {
        var metrics = await _metricsService.GetAggregateMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("mazes/{mazeId}")]
    public async Task<IActionResult> GetMazeMetrics(Guid mazeId)
    {
        var metrics = await _metricsService.GetMazeMetricsAsync(mazeId);
        if (metrics == null)
            return NotFound();
        return Ok(metrics);
    }
}
```

**Step 4: Register MetricsService in DI**

Add to `src/MazeOfHateoas.Api/Program.cs` after other service registrations:

```csharp
builder.Services.AddSingleton<IMetricsService, MetricsService>();
```

Add using:
```csharp
using MazeOfHateoas.Infrastructure;
```

**Step 5: Run tests to verify they pass**

```bash
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~MetricsEndpointTests"
```

Expected: PASS

**Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): add MetricsController with aggregate and maze-specific endpoints"
```

---

### Task 5.2: Configure CORS for dashboard

**Files:**
- Modify: `src/MazeOfHateoas.Api/Program.cs`

**Step 1: Add CORS policy**

In `src/MazeOfHateoas.Api/Program.cs`, after `builder.Services.AddSignalR();` add:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://dashboard:80")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});
```

After `app.UseHttpsRedirection();` add:

```csharp
app.UseCors("Dashboard");
```

**Step 2: Run all tests**

```bash
docker compose -f docker-compose.test.yml up --build
```

Expected: All tests PASS

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(api): configure CORS policy for dashboard"
```

---

## Phase 6: Vue Frontend

### Task 6.1: Initialize Vue project

**Step 1: Create Vue project with Vite**

```bash
cd /Users/cvanlaw/repos/cvanlaw/maze-of-hateoas/.worktrees/dashboard-ui
npm create vite@latest maze-dashboard -- --template vue-ts
cd maze-dashboard
npm install
```

**Step 2: Verify project runs**

```bash
npm run dev -- --host &
sleep 5
curl -s http://localhost:5173 | head -20
kill %1
```

Expected: HTML output with Vite/Vue content

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(dashboard): initialize Vue 3 + TypeScript project with Vite"
```

---

### Task 6.2: Add Tailwind CSS

**Step 1: Install Tailwind**

```bash
cd maze-dashboard
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

**Step 2: Configure Tailwind**

Replace `maze-dashboard/tailwind.config.js`:

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

**Step 3: Add Tailwind directives**

Replace `maze-dashboard/src/style.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

**Step 4: Verify Tailwind works**

```bash
npm run build
```

Expected: Build succeeds

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add Tailwind CSS configuration"
```

---

### Task 6.3: Add SignalR client and Vue Router

**Step 1: Install dependencies**

```bash
cd maze-dashboard
npm install @microsoft/signalr vue-router@4
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add SignalR client and Vue Router dependencies"
```

---

### Task 6.4: Create TypeScript types

**Files:**
- Create: `maze-dashboard/src/types/index.ts`

**Step 1: Create types file**

Create file `maze-dashboard/src/types/index.ts`:

```typescript
export interface AggregateMetrics {
  activeSession: number;
  completedToday: number;
  completionRate: number;
  averageMoves: number;
  mostActiveMazeId: string | null;
  mostActiveMazeSessionCount: number;
  systemVelocity: number;
}

export interface Position {
  x: number;
  y: number;
}

export interface SessionSnapshot {
  sessionId: string;
  currentPosition: Position;
  moveCount: number;
  visitedCount: number;
  completionPercent: number;
  velocity: number;
  duration: string;
}

export interface Cell {
  position: Position;
  hasNorthWall: boolean;
  hasSouthWall: boolean;
  hasEastWall: boolean;
  hasWestWall: boolean;
}

export interface MazeMetrics {
  mazeId: string;
  width: number;
  height: number;
  cells: Cell[][];
  activeSessions: number;
  totalCompleted: number;
  sessions: SessionSnapshot[];
}

export interface MazeSummary {
  id: string;
  width: number;
  height: number;
  createdAt: string;
}

export interface SessionStartedEvent {
  sessionId: string;
  mazeId: string;
  timestamp: string;
}

export interface SessionMovedEvent {
  sessionId: string;
  mazeId: string;
  positionX: number;
  positionY: number;
  moveCount: number;
  visitedCount: number;
}

export interface SessionCompletedEvent {
  sessionId: string;
  mazeId: string;
  moveCount: number;
  duration: string;
}
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add TypeScript type definitions"
```

---

### Task 6.5: Create API service

**Files:**
- Create: `maze-dashboard/src/services/api.ts`

**Step 1: Create API service**

Create file `maze-dashboard/src/services/api.ts`:

```typescript
import type { AggregateMetrics, MazeMetrics, MazeSummary } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export async function fetchAggregateMetrics(): Promise<AggregateMetrics> {
  const response = await fetch(`${API_URL}/api/metrics`);
  if (!response.ok) throw new Error('Failed to fetch aggregate metrics');
  return response.json();
}

export async function fetchMazeMetrics(mazeId: string): Promise<MazeMetrics> {
  const response = await fetch(`${API_URL}/api/metrics/mazes/${mazeId}`);
  if (!response.ok) throw new Error('Failed to fetch maze metrics');
  return response.json();
}

export async function fetchMazes(): Promise<MazeSummary[]> {
  const response = await fetch(`${API_URL}/api/mazes`);
  if (!response.ok) throw new Error('Failed to fetch mazes');
  const data = await response.json();
  return data.mazes || [];
}
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add API service"
```

---

### Task 6.6: Create SignalR composable

**Files:**
- Create: `maze-dashboard/src/composables/useSignalR.ts`

**Step 1: Create SignalR composable**

Create file `maze-dashboard/src/composables/useSignalR.ts`:

```typescript
import { ref, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import type {
  SessionStartedEvent,
  SessionMovedEvent,
  SessionCompletedEvent
} from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export function useSignalR() {
  const connection = ref<signalR.HubConnection | null>(null);
  const isConnected = ref(false);
  const error = ref<string | null>(null);

  const onSessionStarted = ref<((event: SessionStartedEvent) => void) | null>(null);
  const onSessionMoved = ref<((event: SessionMovedEvent) => void) | null>(null);
  const onSessionCompleted = ref<((event: SessionCompletedEvent) => void) | null>(null);

  async function connect() {
    try {
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${API_URL}/hubs/metrics`)
        .withAutomaticReconnect()
        .build();

      connection.value.on('SessionStarted', (event: SessionStartedEvent) => {
        onSessionStarted.value?.(event);
      });

      connection.value.on('SessionMoved', (event: SessionMovedEvent) => {
        onSessionMoved.value?.(event);
      });

      connection.value.on('SessionCompleted', (event: SessionCompletedEvent) => {
        onSessionCompleted.value?.(event);
      });

      await connection.value.start();
      isConnected.value = true;
      error.value = null;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Connection failed';
      isConnected.value = false;
    }
  }

  async function subscribeToAll() {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('SubscribeToAll');
    }
  }

  async function subscribeToMaze(mazeId: string) {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('SubscribeToMaze', mazeId);
    }
  }

  async function unsubscribeFromMaze(mazeId: string) {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('UnsubscribeFromMaze', mazeId);
    }
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop();
      isConnected.value = false;
    }
  }

  onUnmounted(() => {
    disconnect();
  });

  return {
    isConnected,
    error,
    connect,
    disconnect,
    subscribeToAll,
    subscribeToMaze,
    unsubscribeFromMaze,
    onSessionStarted,
    onSessionMoved,
    onSessionCompleted
  };
}
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add SignalR composable for real-time updates"
```

---

### Task 6.7: Create MetricCard component

**Files:**
- Create: `maze-dashboard/src/components/MetricCard.vue`

**Step 1: Create component**

Create file `maze-dashboard/src/components/MetricCard.vue`:

```vue
<script setup lang="ts">
defineProps<{
  label: string;
  value: string | number;
  color?: 'blue' | 'green' | 'amber' | 'purple';
}>();
</script>

<template>
  <div class="bg-slate-800 rounded-lg p-4 border border-slate-700">
    <div class="text-slate-400 text-sm mb-1">{{ label }}</div>
    <div
      class="text-2xl font-bold"
      :class="{
        'text-blue-400': color === 'blue',
        'text-green-400': color === 'green',
        'text-amber-400': color === 'amber',
        'text-purple-400': color === 'purple',
        'text-white': !color
      }"
    >
      {{ value }}
    </div>
  </div>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add MetricCard component"
```

---

### Task 6.8: Create MazeList component

**Files:**
- Create: `maze-dashboard/src/components/MazeList.vue`

**Step 1: Create component**

Create file `maze-dashboard/src/components/MazeList.vue`:

```vue
<script setup lang="ts">
import type { MazeSummary } from '../types';

defineProps<{
  mazes: MazeSummary[];
  sessionCounts: Record<string, number>;
  mostActiveId?: string | null;
}>();

defineEmits<{
  select: [mazeId: string];
}>();
</script>

<template>
  <div class="bg-slate-800 rounded-lg border border-slate-700">
    <div class="p-4 border-b border-slate-700">
      <h2 class="text-lg font-semibold text-white">Active Mazes</h2>
    </div>
    <div class="divide-y divide-slate-700">
      <div
        v-for="maze in mazes"
        :key="maze.id"
        class="p-4 flex items-center justify-between hover:bg-slate-700/50 cursor-pointer transition-colors"
        @click="$emit('select', maze.id)"
      >
        <div>
          <div class="text-white font-medium">
            Maze {{ maze.id.slice(0, 8) }}...
            <span
              v-if="maze.id === mostActiveId"
              class="ml-2 text-xs bg-amber-500/20 text-amber-400 px-2 py-0.5 rounded"
            >
              Most Active
            </span>
          </div>
          <div class="text-slate-400 text-sm">
            {{ maze.width }}x{{ maze.height }}
          </div>
        </div>
        <div class="flex items-center gap-4">
          <div class="text-slate-400">
            {{ sessionCounts[maze.id] || 0 }} sessions
          </div>
          <svg
            class="w-5 h-5 text-slate-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 5l7 7-7 7"
            />
          </svg>
        </div>
      </div>
      <div
        v-if="mazes.length === 0"
        class="p-4 text-slate-400 text-center"
      >
        No mazes available
      </div>
    </div>
  </div>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add MazeList component"
```

---

### Task 6.9: Create AggregateView page

**Files:**
- Create: `maze-dashboard/src/views/AggregateView.vue`

**Step 1: Create view**

Create file `maze-dashboard/src/views/AggregateView.vue`:

```vue
<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import MetricCard from '../components/MetricCard.vue';
import MazeList from '../components/MazeList.vue';
import { useSignalR } from '../composables/useSignalR';
import { fetchAggregateMetrics, fetchMazes } from '../services/api';
import type { AggregateMetrics, MazeSummary } from '../types';

const router = useRouter();
const metrics = ref<AggregateMetrics | null>(null);
const mazes = ref<MazeSummary[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);

const {
  isConnected,
  connect,
  subscribeToAll,
  onSessionStarted,
  onSessionMoved,
  onSessionCompleted
} = useSignalR();

const sessionCounts = ref<Record<string, number>>({});

async function loadData() {
  try {
    loading.value = true;
    const [metricsData, mazesData] = await Promise.all([
      fetchAggregateMetrics(),
      fetchMazes()
    ]);
    metrics.value = metricsData;
    mazes.value = mazesData;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    loading.value = false;
  }
}

function handleMazeSelect(mazeId: string) {
  router.push(`/maze/${mazeId}`);
}

onMounted(async () => {
  await loadData();
  await connect();
  await subscribeToAll();

  onSessionStarted.value = () => {
    loadData();
  };

  onSessionMoved.value = () => {
    loadData();
  };

  onSessionCompleted.value = () => {
    loadData();
  };
});
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <header class="border-b border-slate-700 px-6 py-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold">Maze Dashboard</h1>
        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            :class="isConnected ? 'bg-green-500' : 'bg-red-500'"
          />
          <span class="text-sm text-slate-400">
            {{ isConnected ? 'Live' : 'Disconnected' }}
          </span>
        </div>
      </div>
    </header>

    <main class="p-6">
      <div v-if="loading" class="text-center py-12">
        <div class="text-slate-400">Loading...</div>
      </div>

      <div v-else-if="error" class="text-center py-12">
        <div class="text-red-400">{{ error }}</div>
      </div>

      <template v-else-if="metrics">
        <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
          <MetricCard
            label="Active Sessions"
            :value="metrics.activeSession"
            color="blue"
          />
          <MetricCard
            label="Completed Today"
            :value="metrics.completedToday"
            color="green"
          />
          <MetricCard
            label="Completion Rate"
            :value="`${metrics.completionRate}%`"
            color="green"
          />
          <MetricCard
            label="Avg Moves"
            :value="metrics.averageMoves"
          />
          <MetricCard
            label="Most Active"
            :value="metrics.mostActiveMazeSessionCount"
            color="purple"
          />
          <MetricCard
            label="System Velocity"
            :value="`${metrics.systemVelocity}/m`"
            color="amber"
          />
        </div>

        <MazeList
          :mazes="mazes"
          :session-counts="sessionCounts"
          :most-active-id="metrics.mostActiveMazeId"
          @select="handleMazeSelect"
        />
      </template>
    </main>
  </div>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add AggregateView page"
```

---

### Task 6.10: Create MazeGrid component

**Files:**
- Create: `maze-dashboard/src/components/MazeGrid.vue`

**Step 1: Create component**

Create file `maze-dashboard/src/components/MazeGrid.vue`:

```vue
<script setup lang="ts">
import { computed } from 'vue';
import type { Cell, SessionSnapshot, Position } from '../types';

const props = defineProps<{
  cells: Cell[][];
  width: number;
  height: number;
  sessions: SessionSnapshot[];
  heatmap?: Map<string, number>;
  showHeatmap?: boolean;
}>();

const cellSize = 24;
const wallWidth = 2;

const svgWidth = computed(() => props.width * cellSize + wallWidth);
const svgHeight = computed(() => props.height * cellSize + wallWidth);

const sessionColors = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'
];

function getCellAt(x: number, y: number): Cell | undefined {
  return props.cells[x]?.[y];
}

function getSessionColor(index: number): string {
  return sessionColors[index % sessionColors.length];
}

function posKey(pos: Position): string {
  return `${pos.x},${pos.y}`;
}

function getHeatmapOpacity(x: number, y: number): number {
  if (!props.heatmap || !props.showHeatmap) return 0;
  const count = props.heatmap.get(`${x},${y}`) || 0;
  const maxCount = Math.max(...props.heatmap.values(), 1);
  return count / maxCount * 0.6;
}
</script>

<template>
  <svg
    :width="svgWidth"
    :height="svgHeight"
    class="bg-slate-800 rounded"
  >
    <!-- Grid cells and heatmap -->
    <g v-for="x in width" :key="`col-${x}`">
      <g v-for="y in height" :key="`cell-${x}-${y}`">
        <!-- Heatmap background -->
        <rect
          v-if="showHeatmap"
          :x="(x - 1) * cellSize + wallWidth"
          :y="(y - 1) * cellSize + wallWidth"
          :width="cellSize - wallWidth"
          :height="cellSize - wallWidth"
          fill="#f59e0b"
          :fill-opacity="getHeatmapOpacity(x - 1, y - 1)"
        />

        <!-- Walls -->
        <template v-if="getCellAt(x - 1, y - 1)">
          <!-- North wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasNorthWall"
            :x1="(x - 1) * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="x * cellSize"
            :y2="(y - 1) * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- South wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasSouthWall"
            :x1="(x - 1) * cellSize"
            :y1="y * cellSize"
            :x2="x * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- East wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasEastWall"
            :x1="x * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="x * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- West wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasWestWall"
            :x1="(x - 1) * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="(x - 1) * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
        </template>
      </g>
    </g>

    <!-- Start marker -->
    <text
      :x="cellSize / 2"
      :y="cellSize / 2 + 4"
      text-anchor="middle"
      class="text-xs fill-green-400 font-bold"
    >
      S
    </text>

    <!-- End marker -->
    <text
      :x="(width - 0.5) * cellSize"
      :y="(height - 0.5) * cellSize + 4"
      text-anchor="middle"
      class="text-xs fill-red-400 font-bold"
    >
      E
    </text>

    <!-- Session markers -->
    <circle
      v-for="(session, index) in sessions"
      :key="session.sessionId"
      :cx="session.currentPosition.x * cellSize + cellSize / 2"
      :cy="session.currentPosition.y * cellSize + cellSize / 2"
      r="6"
      :fill="getSessionColor(index)"
      class="transition-all duration-300"
    />
  </svg>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add MazeGrid component with SVG rendering"
```

---

### Task 6.11: Create SessionTable component

**Files:**
- Create: `maze-dashboard/src/components/SessionTable.vue`

**Step 1: Create component**

Create file `maze-dashboard/src/components/SessionTable.vue`:

```vue
<script setup lang="ts">
import type { SessionSnapshot } from '../types';

defineProps<{
  sessions: SessionSnapshot[];
}>();

const sessionColors = [
  'bg-blue-500', 'bg-green-500', 'bg-amber-500', 'bg-red-500',
  'bg-purple-500', 'bg-pink-500', 'bg-cyan-500', 'bg-lime-500'
];

function getSessionColor(index: number): string {
  return sessionColors[index % sessionColors.length];
}

function formatDuration(duration: string): string {
  // Parse ISO 8601 duration or time string
  const match = duration.match(/(\d+):(\d+):(\d+)/);
  if (match) {
    const [, hours, minutes, seconds] = match;
    if (parseInt(hours) > 0) return `${hours}h ${minutes}m`;
    if (parseInt(minutes) > 0) return `${minutes}m ${seconds}s`;
    return `${seconds}s`;
  }
  return duration;
}
</script>

<template>
  <div class="bg-slate-800 rounded-lg border border-slate-700">
    <div class="p-4 border-b border-slate-700">
      <h3 class="font-semibold text-white">
        Sessions ({{ sessions.length }} active)
      </h3>
    </div>
    <div class="max-h-64 overflow-y-auto">
      <div
        v-for="(session, index) in sessions"
        :key="session.sessionId"
        class="px-4 py-2 border-b border-slate-700/50 flex items-center gap-3"
      >
        <div
          class="w-3 h-3 rounded-full"
          :class="getSessionColor(index)"
        />
        <div class="flex-1 min-w-0">
          <div class="text-sm text-white font-mono truncate">
            {{ session.sessionId.slice(0, 8) }}...
          </div>
        </div>
        <div class="text-sm text-slate-400">
          {{ session.completionPercent }}%
        </div>
        <div class="text-sm text-amber-400">
          {{ session.velocity }}/m
        </div>
      </div>
      <div
        v-if="sessions.length === 0"
        class="p-4 text-slate-400 text-center text-sm"
      >
        No active sessions
      </div>
    </div>
  </div>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add SessionTable component"
```

---

### Task 6.12: Create MazeDetailView page

**Files:**
- Create: `maze-dashboard/src/views/MazeDetailView.vue`

**Step 1: Create view**

Create file `maze-dashboard/src/views/MazeDetailView.vue`:

```vue
<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import MazeGrid from '../components/MazeGrid.vue';
import SessionTable from '../components/SessionTable.vue';
import { useSignalR } from '../composables/useSignalR';
import { fetchMazeMetrics } from '../services/api';
import type { MazeMetrics } from '../types';

const route = useRoute();
const router = useRouter();
const mazeId = computed(() => route.params.id as string);

const metrics = ref<MazeMetrics | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const showHeatmap = ref(false);

const {
  isConnected,
  connect,
  subscribeToMaze,
  unsubscribeFromMaze,
  onSessionStarted,
  onSessionMoved,
  onSessionCompleted
} = useSignalR();

const heatmap = computed(() => {
  if (!metrics.value) return new Map<string, number>();
  const map = new Map<string, number>();
  for (const session of metrics.value.sessions) {
    const key = `${session.currentPosition.x},${session.currentPosition.y}`;
    map.set(key, (map.get(key) || 0) + 1);
  }
  return map;
});

async function loadData() {
  try {
    metrics.value = await fetchMazeMetrics(mazeId.value);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    loading.value = false;
  }
}

function goBack() {
  router.push('/');
}

onMounted(async () => {
  await loadData();
  await connect();
  await subscribeToMaze(mazeId.value);

  onSessionStarted.value = (event) => {
    if (event.mazeId === mazeId.value) loadData();
  };

  onSessionMoved.value = (event) => {
    if (event.mazeId === mazeId.value) loadData();
  };

  onSessionCompleted.value = (event) => {
    if (event.mazeId === mazeId.value) loadData();
  };
});

onUnmounted(async () => {
  await unsubscribeFromMaze(mazeId.value);
});
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <header class="border-b border-slate-700 px-6 py-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4">
          <button
            @click="goBack"
            class="text-slate-400 hover:text-white transition-colors"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <h1 class="text-xl font-bold">
            Maze {{ mazeId.slice(0, 8) }}...
            <span v-if="metrics" class="text-slate-400 font-normal">
              ({{ metrics.width }}x{{ metrics.height }})
            </span>
          </h1>
        </div>
        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            :class="isConnected ? 'bg-green-500' : 'bg-red-500'"
          />
          <span class="text-sm text-slate-400">
            {{ isConnected ? 'Live' : 'Disconnected' }}
          </span>
        </div>
      </div>
    </header>

    <main class="p-6">
      <div v-if="loading" class="text-center py-12">
        <div class="text-slate-400">Loading...</div>
      </div>

      <div v-else-if="error" class="text-center py-12">
        <div class="text-red-400">{{ error }}</div>
      </div>

      <template v-else-if="metrics">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2">
            <div class="bg-slate-800 rounded-lg border border-slate-700 p-4">
              <div class="flex items-center justify-between mb-4">
                <h2 class="font-semibold">Maze Grid</h2>
                <label class="flex items-center gap-2 text-sm">
                  <input
                    v-model="showHeatmap"
                    type="checkbox"
                    class="rounded bg-slate-700 border-slate-600"
                  />
                  Show Heatmap
                </label>
              </div>
              <div class="flex justify-center overflow-auto">
                <MazeGrid
                  :cells="metrics.cells"
                  :width="metrics.width"
                  :height="metrics.height"
                  :sessions="metrics.sessions"
                  :heatmap="heatmap"
                  :show-heatmap="showHeatmap"
                />
              </div>
            </div>
          </div>

          <div>
            <SessionTable :sessions="metrics.sessions" />
          </div>
        </div>
      </template>
    </main>
  </div>
</template>
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add MazeDetailView page"
```

---

### Task 6.13: Configure Vue Router

**Files:**
- Create: `maze-dashboard/src/router/index.ts`
- Modify: `maze-dashboard/src/main.ts`
- Modify: `maze-dashboard/src/App.vue`

**Step 1: Create router configuration**

Create file `maze-dashboard/src/router/index.ts`:

```typescript
import { createRouter, createWebHistory } from 'vue-router';
import AggregateView from '../views/AggregateView.vue';
import MazeDetailView from '../views/MazeDetailView.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: AggregateView
    },
    {
      path: '/maze/:id',
      name: 'maze-detail',
      component: MazeDetailView
    }
  ]
});

export default router;
```

**Step 2: Update main.ts**

Replace `maze-dashboard/src/main.ts`:

```typescript
import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import './style.css';

createApp(App).use(router).mount('#app');
```

**Step 3: Update App.vue**

Replace `maze-dashboard/src/App.vue`:

```vue
<script setup lang="ts">
</script>

<template>
  <RouterView />
</template>
```

**Step 4: Verify build succeeds**

```bash
cd maze-dashboard && npm run build
```

Expected: Build succeeds

**Step 5: Commit**

```bash
git add -A && git commit -m "feat(dashboard): configure Vue Router with routes"
```

---

## Phase 7: Docker Integration

### Task 7.1: Create Dockerfile for dashboard

**Files:**
- Create: `maze-dashboard/Dockerfile`

**Step 1: Create Dockerfile**

Create file `maze-dashboard/Dockerfile`:

```dockerfile
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

**Step 2: Create nginx config**

Create file `maze-dashboard/nginx.conf`:

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

**Step 3: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add Dockerfile and nginx config"
```

---

### Task 7.2: Add dashboard to docker-compose

**Files:**
- Modify: `docker-compose.yml`

**Step 1: Add dashboard service**

Add to `docker-compose.yml` services section:

```yaml
  dashboard:
    build:
      context: ./maze-dashboard
      dockerfile: Dockerfile
    ports:
      - "5173:80"
    environment:
      - VITE_API_URL=http://localhost:8080
    depends_on:
      - api
```

**Step 2: Test docker-compose**

```bash
docker compose up --build -d
sleep 10
curl -s http://localhost:8080/health
curl -s http://localhost:5173 | head -10
docker compose down
```

Expected: Both services respond

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add dashboard service to docker-compose"
```

---

### Task 7.3: Update environment variables for production build

**Files:**
- Create: `maze-dashboard/.env.production`

**Step 1: Create production env file**

Create file `maze-dashboard/.env.production`:

```
VITE_API_URL=http://localhost:8080
```

**Step 2: Commit**

```bash
git add -A && git commit -m "feat(dashboard): add production environment configuration"
```

---

## Phase 8: Final Integration Testing

### Task 8.1: Run all tests

**Step 1: Run backend tests**

```bash
docker compose -f docker-compose.test.yml up --build
```

Expected: All tests PASS

**Step 2: Run frontend build**

```bash
cd maze-dashboard && npm run build
```

Expected: Build succeeds with no errors

**Step 3: Run full stack**

```bash
docker compose up --build -d
sleep 10
# Create a maze
curl -X POST http://localhost:8080/api/mazes
# Check metrics endpoint
curl http://localhost:8080/api/metrics
# Check dashboard loads
curl -s http://localhost:5173 | grep -o "Maze Dashboard" || echo "Dashboard HTML loaded"
docker compose down
```

**Step 4: Commit any final fixes**

```bash
git add -A && git commit -m "test: verify full stack integration"
```

---

## Summary

**Total Tasks:** 27
**Estimated Implementation:** Follow TDD for each task

**Key Files Created:**
- Domain: `MazeSession.cs` (modified)
- Application: `DTOs/*.cs`, `IMetricsService.cs`
- Infrastructure: `MetricsService.cs`
- API: `MetricsController.cs`, `Hubs/MetricsHub.cs`, `Hubs/HubEvents.cs`
- Frontend: Full `maze-dashboard/` Vue 3 application

**Run Order:**
1. Phase 1-5: Backend changes (TDD with Docker tests)
2. Phase 6: Frontend development
3. Phase 7: Docker integration
4. Phase 8: Full integration testing
