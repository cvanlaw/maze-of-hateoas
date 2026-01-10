using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests;

public class CellTests
{
    [Fact]
    public void Cell_HasPositionProperty()
    {
        var position = new Position(3, 5);
        var cell = new Cell(position, false, false, false, false);
        Assert.Equal(position, cell.Position);
    }

    [Fact]
    public void Cell_HasWallProperties()
    {
        var cell = new Cell(new Position(0, 0), true, true, true, true);
        Assert.True(cell.HasNorthWall);
        Assert.True(cell.HasSouthWall);
        Assert.True(cell.HasEastWall);
        Assert.True(cell.HasWestWall);
    }

    [Fact]
    public void CanMove_North_ReturnsFalse_WhenHasNorthWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: true, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        Assert.False(cell.CanMove(Direction.North));
    }

    [Fact]
    public void CanMove_North_ReturnsTrue_WhenNoNorthWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: true, HasEastWall: true, HasWestWall: true);
        Assert.True(cell.CanMove(Direction.North));
    }

    [Fact]
    public void CanMove_South_ReturnsFalse_WhenHasSouthWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: true, HasEastWall: false, HasWestWall: false);
        Assert.False(cell.CanMove(Direction.South));
    }

    [Fact]
    public void CanMove_South_ReturnsTrue_WhenNoSouthWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: true, HasSouthWall: false, HasEastWall: true, HasWestWall: true);
        Assert.True(cell.CanMove(Direction.South));
    }

    [Fact]
    public void CanMove_East_ReturnsFalse_WhenHasEastWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: true, HasWestWall: false);
        Assert.False(cell.CanMove(Direction.East));
    }

    [Fact]
    public void CanMove_East_ReturnsTrue_WhenNoEastWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: true, HasSouthWall: true, HasEastWall: false, HasWestWall: true);
        Assert.True(cell.CanMove(Direction.East));
    }

    [Fact]
    public void CanMove_West_ReturnsFalse_WhenHasWestWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: true);
        Assert.False(cell.CanMove(Direction.West));
    }

    [Fact]
    public void CanMove_West_ReturnsTrue_WhenNoWestWall()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: true, HasSouthWall: true, HasEastWall: true, HasWestWall: false);
        Assert.True(cell.CanMove(Direction.West));
    }

    [Theory]
    [InlineData(Direction.North, true, false, false, false, false)]
    [InlineData(Direction.North, false, true, true, true, true)]
    [InlineData(Direction.South, false, true, false, false, false)]
    [InlineData(Direction.South, true, false, true, true, true)]
    [InlineData(Direction.East, false, false, true, false, false)]
    [InlineData(Direction.East, true, true, false, true, true)]
    [InlineData(Direction.West, false, false, false, true, false)]
    [InlineData(Direction.West, true, true, true, false, true)]
    public void CanMove_ReturnsExpectedResult(Direction direction, bool hasNorth, bool hasSouth, bool hasEast, bool hasWest, bool expectedCanMove)
    {
        var cell = new Cell(new Position(0, 0), hasNorth, hasSouth, hasEast, hasWest);
        Assert.Equal(expectedCanMove, cell.CanMove(direction));
    }
}
