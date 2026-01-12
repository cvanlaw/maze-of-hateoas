# Solver Algorithm Modularity Design

## Overview

Refactor the maze solver to support multiple solving algorithms, selectable via configuration.

## Algorithms

| Algorithm | Class | Behavior |
|-----------|-------|----------|
| `dfs` | `DepthFirstSolver` | Stack-based depth-first search with backtracking. Current implementation. |
| `bfs` | `BreadthFirstSolver` | Queue-based breadth-first search. Finds shortest path. |
| `random` | `RandomWalkSolver` | Random selection among unvisited neighbors, backtrack when stuck. |

All algorithms guarantee completion via backtracking.

## Configuration

### SolverSettings

Add `Algorithm` property:

```csharp
public class SolverSettings
{
    // ... existing properties ...
    public string Algorithm { get; set; } = "dfs";
}
```

### appsettings.json

```json
{
  "Solver": {
    "Algorithm": "dfs"
  }
}
```

### Environment Variable Override

`SOLVER_ALGORITHM=bfs` overrides the config file value.

Priority: Environment variable > appsettings.json > default ("dfs")

## Implementation

### File Changes

| File | Change |
|------|--------|
| `Configuration/SolverSettings.cs` | Add `Algorithm` property |
| `Services/HateoasSolver.cs` | Rename to `DepthFirstSolver.cs` |
| `Services/BreadthFirstSolver.cs` | New file |
| `Services/RandomWalkSolver.cs` | New file |
| `Program.cs` | Factory lambda for solver registration, add appsettings.json support |
| `appsettings.json` | New file with default configuration |

### Solver Registration

```csharp
builder.Services.AddSingleton<ISolver>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<SolverSettings>>().Value;
    var logger = sp.GetRequiredService<ILoggerFactory>();
    var apiClient = sp.GetRequiredService<IMazeApiClient>();

    return settings.Algorithm.ToLowerInvariant() switch
    {
        "bfs" => new BreadthFirstSolver(apiClient, ...),
        "random" => new RandomWalkSolver(apiClient, ...),
        _ => new DepthFirstSolver(apiClient, ...)
    };
});
```

Invalid algorithm values fall back to DFS with a warning log.

### Algorithm Details

**DepthFirstSolver**: Current `HateoasSolver` logic unchanged. Explores depth-first using a stack, backtracks on dead ends.

**BreadthFirstSolver**: Uses a queue instead of stack. Tracks path to each cell to enable backtracking. Explores all cells at distance N before N+1.

**RandomWalkSolver**: Like DFS but shuffles unvisited neighbors before selecting. Same backtracking mechanism ensures completion.

## Testing

### Unit Tests

Each solver needs tests for:
- Simple path completion
- Dead-end backtracking
- Already-at-end case (immediate completion)
- Visited cell tracking

### Configuration Tests

- Algorithm selection from appsettings.json
- Environment variable override precedence
- Invalid algorithm falls back to DFS

### Test Files

| File | Purpose |
|------|---------|
| `DepthFirstSolverTests.cs` | Renamed from `HateoasSolverTests.cs` |
| `BreadthFirstSolverTests.cs` | New tests for BFS |
| `RandomWalkSolverTests.cs` | New tests for random walk |
| `SolverSettingsTests.cs` | Add algorithm configuration tests |
