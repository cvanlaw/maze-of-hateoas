namespace MazeOfHateoas.Domain;

public readonly record struct Cell(
    Position Position,
    bool HasNorthWall,
    bool HasSouthWall,
    bool HasEastWall,
    bool HasWestWall)
{
    public bool CanMove(Direction direction) => direction switch
    {
        Direction.North => !HasNorthWall,
        Direction.South => !HasSouthWall,
        Direction.East => !HasEastWall,
        Direction.West => !HasWestWall,
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };
}
