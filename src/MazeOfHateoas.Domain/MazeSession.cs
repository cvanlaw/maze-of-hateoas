namespace MazeOfHateoas.Domain;

public class MazeSession
{
    public Guid Id { get; }
    public Guid MazeId { get; }
    public Position CurrentPosition { get; private set; }
    public SessionState State { get; private set; }
    public DateTime StartedAt { get; }
    public int MoveCount { get; private set; }
    public HashSet<Position> VisitedCells { get; } = new();

    public MazeSession(Guid id, Guid mazeId, Position startPosition)
    {
        Id = id;
        MazeId = mazeId;
        CurrentPosition = startPosition;
        State = SessionState.InProgress;
        StartedAt = DateTime.UtcNow;
        VisitedCells.Add(startPosition);
    }

    public MoveResult Move(Direction direction, Maze maze)
    {
        if (State == SessionState.Completed)
            return MoveResult.AlreadyCompleted;

        var cell = maze.Cells[CurrentPosition.X, CurrentPosition.Y];
        if (!cell.CanMove(direction))
            return MoveResult.Blocked;

        var newPosition = CurrentPosition.Move(direction);
        if (!IsWithinBounds(newPosition, maze))
            return MoveResult.OutOfBounds;

        CurrentPosition = newPosition;
        MoveCount++;
        VisitedCells.Add(CurrentPosition);

        if (CurrentPosition == maze.End)
            State = SessionState.Completed;

        return MoveResult.Success;
    }

    private static bool IsWithinBounds(Position position, Maze maze)
    {
        return position.X >= 0 && position.X < maze.Width &&
               position.Y >= 0 && position.Y < maze.Height;
    }
}
