namespace MazeOfHateoas.Domain;

public enum MoveResult
{
    Success,
    Blocked,
    OutOfBounds,
    AlreadyCompleted
}
