using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests;

public class DirectionTests
{
    [Fact]
    public void Direction_HasNorthValue()
    {
        var direction = Direction.North;
        Assert.Equal(Direction.North, direction);
    }

    [Fact]
    public void Direction_HasSouthValue()
    {
        var direction = Direction.South;
        Assert.Equal(Direction.South, direction);
    }

    [Fact]
    public void Direction_HasEastValue()
    {
        var direction = Direction.East;
        Assert.Equal(Direction.East, direction);
    }

    [Fact]
    public void Direction_HasWestValue()
    {
        var direction = Direction.West;
        Assert.Equal(Direction.West, direction);
    }

    [Fact]
    public void Direction_HasExactlyFourValues()
    {
        var values = Enum.GetValues<Direction>();
        Assert.Equal(4, values.Length);
    }
}
