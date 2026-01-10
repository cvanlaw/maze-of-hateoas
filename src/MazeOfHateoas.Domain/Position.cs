namespace MazeOfHateoas.Domain;

public readonly record struct Position(int X, int Y)
{
    public Position Move(Direction direction) => direction switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.East => this with { X = X + 1 },
        Direction.West => this with { X = X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };
}
