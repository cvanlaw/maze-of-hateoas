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
