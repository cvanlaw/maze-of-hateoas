using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests;

public class PositionTests
{
    [Fact]
    public void Position_HasXAndYProperties()
    {
        var position = new Position(3, 5);
        Assert.Equal(3, position.X);
        Assert.Equal(5, position.Y);
    }

    [Theory]
    [InlineData(Direction.North, 5, 4)]
    [InlineData(Direction.South, 5, 6)]
    [InlineData(Direction.East, 6, 5)]
    [InlineData(Direction.West, 4, 5)]
    public void Move_ReturnsCorrectPosition(Direction direction, int expectedX, int expectedY)
    {
        var position = new Position(5, 5);
        var result = position.Move(direction);
        Assert.Equal(new Position(expectedX, expectedY), result);
    }

    [Fact]
    public void Move_North_DecreasesY()
    {
        var position = new Position(0, 10);
        var result = position.Move(Direction.North);
        Assert.Equal(9, result.Y);
        Assert.Equal(0, result.X);
    }

    [Fact]
    public void Move_South_IncreasesY()
    {
        var position = new Position(0, 10);
        var result = position.Move(Direction.South);
        Assert.Equal(11, result.Y);
        Assert.Equal(0, result.X);
    }

    [Fact]
    public void Move_East_IncreasesX()
    {
        var position = new Position(10, 0);
        var result = position.Move(Direction.East);
        Assert.Equal(11, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void Move_West_DecreasesX()
    {
        var position = new Position(10, 0);
        var result = position.Move(Direction.West);
        Assert.Equal(9, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void Position_IsValueEqual()
    {
        var pos1 = new Position(3, 4);
        var pos2 = new Position(3, 4);
        Assert.Equal(pos1, pos2);
    }
}
