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
