using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests;

public class MazeTests
{
    [Fact]
    public void Maze_HasRequiredProperties()
    {
        var cells = new Cell[5, 5];
        var start = new Position(0, 0);
        var end = new Position(4, 4);
        var createdAt = DateTime.UtcNow;
        var id = Guid.NewGuid();

        var maze = new Maze(id, 5, 5, cells, start, end, createdAt);

        Assert.Equal(id, maze.Id);
        Assert.Equal(5, maze.Width);
        Assert.Equal(5, maze.Height);
        Assert.Equal(cells, maze.Cells);
        Assert.Equal(start, maze.Start);
        Assert.Equal(end, maze.End);
        Assert.Equal(createdAt, maze.CreatedAt);
    }

    [Fact]
    public void Maze_GetCell_ReturnsCorrectCell()
    {
        var cells = new Cell[3, 3];
        var expectedCell = new Cell(new Position(1, 2), true, false, true, false);
        cells[1, 2] = expectedCell;

        var maze = new Maze(Guid.NewGuid(), 3, 3, cells, new Position(0, 0), new Position(2, 2), DateTime.UtcNow);

        Assert.Equal(expectedCell, maze.GetCell(1, 2));
    }
}
