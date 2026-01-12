using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.DTOs;

public record MazeMetrics(
    Guid MazeId,
    int Width,
    int Height,
    Cell[][] Cells,
    int ActiveSessions,
    int TotalCompleted,
    List<SessionSnapshot> Sessions
);
