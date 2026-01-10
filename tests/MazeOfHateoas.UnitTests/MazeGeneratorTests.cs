using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using MazeOfHateoas.Infrastructure;

namespace MazeOfHateoas.UnitTests;

public class MazeGeneratorTests
{
    private readonly IMazeGenerator _generator = new MazeGenerator();

    [Fact]
    public void Generate_CreatesMazeWithCorrectDimensions()
    {
        var maze = _generator.Generate(5, 7);

        Assert.Equal(5, maze.Width);
        Assert.Equal(7, maze.Height);
    }

    [Fact]
    public void Generate_StartIsAtOrigin()
    {
        var maze = _generator.Generate(10, 10);

        Assert.Equal(new Position(0, 0), maze.Start);
    }

    [Fact]
    public void Generate_EndIsAtBottomRight()
    {
        var maze = _generator.Generate(10, 10);

        Assert.Equal(new Position(9, 9), maze.End);
    }

    [Fact]
    public void Generate_AssignsUniqueId()
    {
        var maze1 = _generator.Generate(5, 5);
        var maze2 = _generator.Generate(5, 5);

        Assert.NotEqual(Guid.Empty, maze1.Id);
        Assert.NotEqual(maze1.Id, maze2.Id);
    }

    [Fact]
    public void Generate_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var maze = _generator.Generate(5, 5);
        var after = DateTime.UtcNow;

        Assert.InRange(maze.CreatedAt, before, after);
    }

    [Fact]
    public void Generate_WithSeededRandom_ProducesDeterministicMaze()
    {
        var random1 = new Random(42);
        var random2 = new Random(42);

        var maze1 = _generator.Generate(5, 5, random1);
        var maze2 = _generator.Generate(5, 5, random2);

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var cell1 = maze1.GetCell(x, y);
                var cell2 = maze2.GetCell(x, y);
                Assert.Equal(cell1.HasNorthWall, cell2.HasNorthWall);
                Assert.Equal(cell1.HasSouthWall, cell2.HasSouthWall);
                Assert.Equal(cell1.HasEastWall, cell2.HasEastWall);
                Assert.Equal(cell1.HasWestWall, cell2.HasWestWall);
            }
        }
    }

    [Fact]
    public void Generate_CreatesSolvableMaze()
    {
        var maze = _generator.Generate(10, 10, new Random(123));

        Assert.True(IsSolvable(maze), "Maze should be solvable (path from start to end)");
    }

    [Fact]
    public void Generate_AllCellsHaveCorrectPositions()
    {
        var maze = _generator.Generate(5, 5);

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var cell = maze.GetCell(x, y);
                Assert.Equal(new Position(x, y), cell.Position);
            }
        }
    }

    [Fact]
    public void Generate_BorderCellsHaveOuterWalls()
    {
        var maze = _generator.Generate(5, 5);

        for (int x = 0; x < 5; x++)
        {
            Assert.True(maze.GetCell(x, 0).HasNorthWall, $"Cell ({x},0) should have north wall");
            Assert.True(maze.GetCell(x, 4).HasSouthWall, $"Cell ({x},4) should have south wall");
        }

        for (int y = 0; y < 5; y++)
        {
            Assert.True(maze.GetCell(0, y).HasWestWall, $"Cell (0,{y}) should have west wall");
            Assert.True(maze.GetCell(4, y).HasEastWall, $"Cell (4,{y}) should have east wall");
        }
    }

    private static bool IsSolvable(Maze maze)
    {
        var visited = new bool[maze.Width, maze.Height];
        var queue = new Queue<Position>();
        queue.Enqueue(maze.Start);
        visited[maze.Start.X, maze.Start.Y] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == maze.End)
                return true;

            var cell = maze.GetCell(current.X, current.Y);

            foreach (var direction in Enum.GetValues<Direction>())
            {
                if (!cell.CanMove(direction))
                    continue;

                var next = current.Move(direction);

                if (next.X < 0 || next.X >= maze.Width || next.Y < 0 || next.Y >= maze.Height)
                    continue;

                if (visited[next.X, next.Y])
                    continue;

                visited[next.X, next.Y] = true;
                queue.Enqueue(next);
            }
        }

        return false;
    }
}
