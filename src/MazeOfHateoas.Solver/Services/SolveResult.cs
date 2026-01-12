namespace MazeOfHateoas.Solver.Services;

public record SolveResult(
    Guid MazeId,
    Guid SessionId,
    int MoveCount,
    long ElapsedMs,
    bool Success
);
