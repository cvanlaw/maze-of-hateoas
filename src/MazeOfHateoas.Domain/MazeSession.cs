namespace MazeOfHateoas.Domain;

public class MazeSession
{
    public Guid Id { get; }
    public Guid MazeId { get; }
    public Position CurrentPosition { get; private set; }
    public SessionState State { get; private set; }
    public DateTime StartedAt { get; }

    public MazeSession(Guid id, Guid mazeId, Position startPosition)
    {
        Id = id;
        MazeId = mazeId;
        CurrentPosition = startPosition;
        State = SessionState.InProgress;
        StartedAt = DateTime.UtcNow;
    }
}
