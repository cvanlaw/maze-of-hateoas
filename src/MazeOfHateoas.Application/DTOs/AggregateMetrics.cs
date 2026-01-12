namespace MazeOfHateoas.Application.DTOs;

public record AggregateMetrics(
    int ActiveSessions,
    int CompletedToday,
    double CompletionRate,
    double AverageMoves,
    Guid? MostActiveMazeId,
    int MostActiveMazeSessionCount,
    double SystemVelocity,
    Dictionary<Guid, int> SessionCountsByMaze
);
