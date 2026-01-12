# Maze Dashboard UI Design

## Overview

A standalone Vue 3 SPA for visualizing maze API clients in real-time. Displays aggregate metrics across all mazes and per-maze detail views with live client position tracking.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Vue 3 SPA (Client)                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  Aggregate   │  │  Maze Detail │  │  WebSocket       │  │
│  │  Dashboard   │  │  View        │  │  Service         │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ SignalR WebSocket + REST
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    .NET API (Server)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  Existing    │  │  SignalR Hub │  │  Metrics         │  │
│  │  Controllers │  │  (new)       │  │  Service (new)   │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack

- **Frontend**: Vue 3 + TypeScript + Tailwind CSS + Vite
- **Real-time**: SignalR WebSocket
- **API**: Existing .NET API extended with SignalR hub and metrics service
- **Authentication**: None (open dashboard)

### Communication

- REST API for initial data fetch (maze list, session details, metrics)
- SignalR WebSocket for real-time session updates (moves, completions, new sessions)

## Domain Model Changes

The `MazeSession` entity needs two new properties:

```csharp
public class MazeSession
{
    // Existing
    public Guid Id { get; }
    public Guid MazeId { get; }
    public Position CurrentPosition { get; private set; }
    public SessionState State { get; private set; }
    public DateTime StartedAt { get; }

    // New
    public int MoveCount { get; private set; }
    public HashSet<Position> VisitedCells { get; }
}
```

### New Properties

- **MoveCount**: Incremented on every successful move. Used for velocity calculation (MoveCount / minutes elapsed) and "average moves to completion" metric.
- **VisitedCells**: Set of unique positions visited. Initialized with start position. Updated on each successful move.

### Derived Metrics

- **Completion percentage**: `VisitedCells.Count / (Maze.Width * Maze.Height) * 100`
- **Velocity**: `MoveCount / (Now - StartedAt).TotalMinutes` (moves per minute)

### Impact

- `MazeSession.Move()` method updated to increment counter and add to visited set
- Session response DTOs extended with new fields
- No breaking changes to existing API contracts (additive only)

## SignalR Real-Time Infrastructure

### MetricsHub

```csharp
public class MetricsHub : Hub
{
    public async Task SubscribeToMaze(Guid mazeId)
    public async Task SubscribeToAll()
    public async Task Unsubscribe()
}
```

### Events

| Event | Payload | Trigger |
|-------|---------|---------|
| `SessionStarted` | sessionId, mazeId, timestamp | New session created |
| `SessionMoved` | sessionId, mazeId, position, moveCount, visitedCount | Any move |
| `SessionCompleted` | sessionId, mazeId, moveCount, duration | Session reaches exit |

### Integration

Events fired from existing controller actions:
- `SessionsController.CreateSession()` → broadcasts `SessionStarted`
- `SessionsController.Move()` → broadcasts `SessionMoved` or `SessionCompleted`

### Hub Groups

- `all` - receives everything (for aggregate view)
- `maze:{mazeId}` - receives events for specific maze (for detail view)

## Metrics Service

### Interface

```csharp
public interface IMetricsService
{
    Task<AggregateMetrics> GetAggregateMetricsAsync();
    Task<MazeMetrics> GetMazeMetricsAsync(Guid mazeId);
}
```

### AggregateMetrics

```csharp
public record AggregateMetrics(
    int ActiveSessions,
    int CompletedToday,
    double CompletionRate,        // % of all sessions that completed
    double AverageMoves,          // avg moves for completed sessions
    Guid? MostActiveMazeId,
    string? MostActiveMazeName,
    double SystemVelocity         // total moves/minute across active sessions
);
```

### MazeMetrics

```csharp
public record MazeMetrics(
    Guid MazeId,
    int Width,
    int Height,
    int ActiveSessions,
    int TotalCompleted,
    List<SessionSnapshot> Sessions
);

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

### New REST Endpoints

- `GET /api/metrics` → returns `AggregateMetrics`
- `GET /api/metrics/mazes/{mazeId}` → returns `MazeMetrics` with full maze grid data

## Vue Frontend Structure

```
maze-dashboard/
├── src/
│   ├── components/
│   │   ├── AggregateView.vue      # Main dashboard with metric cards
│   │   ├── MetricCard.vue         # Reusable stat card component
│   │   ├── MazeList.vue           # Clickable list of mazes
│   │   ├── MazeDetailView.vue     # Per-maze drill-down
│   │   ├── MazeGrid.vue           # Visual maze renderer with positions
│   │   ├── HeatmapOverlay.vue     # Cell visit frequency overlay
│   │   └── SessionTable.vue       # List of active sessions in a maze
│   ├── composables/
│   │   ├── useSignalR.ts          # WebSocket connection management
│   │   ├── useMetrics.ts          # Reactive metrics state
│   │   └── useMazeRenderer.ts     # Canvas/SVG maze drawing logic
│   ├── services/
│   │   └── api.ts                 # REST API client
│   ├── types/
│   │   └── index.ts               # TypeScript interfaces mirroring API
│   ├── App.vue
│   └── main.ts
├── index.html
├── package.json
├── tailwind.config.js
├── tsconfig.json
└── vite.config.ts
```

### Routing

- `/` → AggregateView (dashboard home)
- `/maze/:id` → MazeDetailView (drill-down)

### State Management

Vue 3 composables with `ref`/`reactive`. The `useMetrics` composable holds reactive state updated by SignalR events.

## UI Layout

### Aggregate View

```
┌─────────────────────────────────────────────────────────────┐
│  Maze Dashboard                                    [live 🟢] │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌────────┐ │
│  │ Active  │ │Completed│ │  Comp.  │ │  Avg    │ │ System │ │
│  │Sessions │ │ Today   │ │  Rate   │ │ Moves   │ │Velocity│ │
│  │   42    │ │   187   │ │  73.2%  │ │   84    │ │ 156/m  │ │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └────────┘ │
├─────────────────────────────────────────────────────────────┤
│  Active Mazes                                               │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Maze #1 (10x10)    12 sessions    →                     ││
│  │ Maze #2 (15x15)     8 sessions    →                     ││
│  │ Maze #3 (20x20)    22 sessions    → (most active)       ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Maze Detail View

```
┌─────────────────────────────────────────────────────────────┐
│  ← Back    Maze #3 (20x20)                         [live 🟢] │
├──────────────────────────────────┬──────────────────────────┤
│                                  │  Sessions (22 active)    │
│     ┌───┬───┬───┬───┬───┐       │  ┌──────────────────────┐│
│     │ S │   │   │ █ │   │       │  │ 🔵 abc123  45% 12/m ││
│     ├───┼───┼───┼───┼───┤       │  │ 🟢 def456  78% 8/m  ││
│     │   │ █ │ 🔵│   │   │       │  │ 🟡 ghi789  23% 15/m ││
│     ├───┼───┼───┼───┼───┤       │  │ 🔴 jkl012  91% 3/m  ││
│     │   │   │   │ 🟢│ █ │       │  │ ...                  ││
│     ├───┼───┼───┼───┼───┤       │  └──────────────────────┘│
│     │ █ │   │   │   │   │       ├──────────────────────────┤
│     ├───┼───┼───┼───┼───┤       │  View Options            │
│     │   │   │ █ │   │ E │       │  ○ Live positions        │
│     └───┴───┴───┴───┴───┘       │  ○ Heatmap overlay       │
│                                  │  □ Show walls           │
│     [Zoom +] [Zoom -] [Fit]     │  □ Show trail lines     │
└──────────────────────────────────┴──────────────────────────┘
```

### Visual Design

- **Color scheme**: Dark background (slate-900), accent cards with subtle borders
- **Metrics colors**: Green for positive, amber for velocity, blue for counts
- **Grid rendering**: Canvas-based for performance with larger mazes
- **Session markers**: Colored circles, animate smoothly on position updates
- **Heatmap mode**: Cell shading based on visit frequency across all sessions

## Deployment & Configuration

### CORS (API)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
        policy.WithOrigins("http://localhost:5173", "https://your-dashboard-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});
```

### Docker

```yaml
dashboard:
  build: ./maze-dashboard
  ports:
    - "5173:80"
  environment:
    - VITE_API_URL=http://api:8080
```

### Environment Variables (Vue)

- `VITE_API_URL` - Base URL for REST API
- `VITE_SIGNALR_URL` - WebSocket endpoint (defaults to `${API_URL}/hubs/metrics`)

### Testing

- **Vue components**: Vitest + Vue Test Utils
- **API changes**: Extend existing xUnit tests
- **SignalR hub**: Integration tests with test client
- **E2E**: Playwright (optional)
