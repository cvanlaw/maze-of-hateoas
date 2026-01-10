using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Domain;

public class MazeSessionMoveTests
{
    private static Maze CreateSimpleMaze(int width = 5, int height = 5, Position? start = null, Position? end = null)
    {
        var cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), false, false, false, false);
            }
        }
        return new Maze(
            Guid.NewGuid(),
            width,
            height,
            cells,
            start ?? new Position(0, 0),
            end ?? new Position(width - 1, height - 1),
            DateTime.UtcNow);
    }

    private static Maze CreateMazeWithBlockedCell(int x, int y, bool northWall = false, bool southWall = false, bool eastWall = false, bool westWall = false)
    {
        var cells = new Cell[5, 5];
        for (int cx = 0; cx < 5; cx++)
        {
            for (int cy = 0; cy < 5; cy++)
            {
                if (cx == x && cy == y)
                {
                    cells[cx, cy] = new Cell(new Position(cx, cy), northWall, southWall, eastWall, westWall);
                }
                else
                {
                    cells[cx, cy] = new Cell(new Position(cx, cy), false, false, false, false);
                }
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, new Position(0, 0), new Position(4, 4), DateTime.UtcNow);
    }

    [Fact]
    public void Move_WhenSessionAlreadyCompleted_ReturnsAlreadyCompleted()
    {
        var maze = CreateSimpleMaze(start: new Position(0, 0), end: new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));

        session.Move(Direction.East, maze);

        var result = session.Move(Direction.West, maze);

        Assert.Equal(MoveResult.AlreadyCompleted, result);
    }

    [Fact]
    public void Move_WhenWallBlocksNorth_ReturnsBlocked()
    {
        var maze = CreateMazeWithBlockedCell(2, 2, northWall: true);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.North, maze);

        Assert.Equal(MoveResult.Blocked, result);
    }

    [Fact]
    public void Move_WhenWallBlocksSouth_ReturnsBlocked()
    {
        var maze = CreateMazeWithBlockedCell(2, 2, southWall: true);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.South, maze);

        Assert.Equal(MoveResult.Blocked, result);
    }

    [Fact]
    public void Move_WhenWallBlocksEast_ReturnsBlocked()
    {
        var maze = CreateMazeWithBlockedCell(2, 2, eastWall: true);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.East, maze);

        Assert.Equal(MoveResult.Blocked, result);
    }

    [Fact]
    public void Move_WhenWallBlocksWest_ReturnsBlocked()
    {
        var maze = CreateMazeWithBlockedCell(2, 2, westWall: true);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.West, maze);

        Assert.Equal(MoveResult.Blocked, result);
    }

    [Fact]
    public void Move_WhenMoveNorthWouldExitBounds_ReturnsOutOfBounds()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 0));

        var result = session.Move(Direction.North, maze);

        Assert.Equal(MoveResult.OutOfBounds, result);
    }

    [Fact]
    public void Move_WhenMoveSouthWouldExitBounds_ReturnsOutOfBounds()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 4));

        var result = session.Move(Direction.South, maze);

        Assert.Equal(MoveResult.OutOfBounds, result);
    }

    [Fact]
    public void Move_WhenMoveEastWouldExitBounds_ReturnsOutOfBounds()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(4, 2));

        var result = session.Move(Direction.East, maze);

        Assert.Equal(MoveResult.OutOfBounds, result);
    }

    [Fact]
    public void Move_WhenMoveWestWouldExitBounds_ReturnsOutOfBounds()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 2));

        var result = session.Move(Direction.West, maze);

        Assert.Equal(MoveResult.OutOfBounds, result);
    }

    [Fact]
    public void Move_WhenValidMoveNorth_ReturnsSuccess()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.North, maze);

        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void Move_WhenValidMoveNorth_UpdatesPosition()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.North, maze);

        Assert.Equal(new Position(2, 1), session.CurrentPosition);
    }

    [Fact]
    public void Move_WhenValidMoveSouth_ReturnsSuccess()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.South, maze);

        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void Move_WhenValidMoveSouth_UpdatesPosition()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.South, maze);

        Assert.Equal(new Position(2, 3), session.CurrentPosition);
    }

    [Fact]
    public void Move_WhenValidMoveEast_ReturnsSuccess()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.East, maze);

        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void Move_WhenValidMoveEast_UpdatesPosition()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.East, maze);

        Assert.Equal(new Position(3, 2), session.CurrentPosition);
    }

    [Fact]
    public void Move_WhenValidMoveWest_ReturnsSuccess()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.West, maze);

        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void Move_WhenValidMoveWest_UpdatesPosition()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.West, maze);

        Assert.Equal(new Position(1, 2), session.CurrentPosition);
    }

    [Fact]
    public void Move_WhenReachesEnd_SetsStateToCompleted()
    {
        var maze = CreateSimpleMaze(end: new Position(3, 2));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.East, maze);

        Assert.Equal(SessionState.Completed, session.State);
    }

    [Fact]
    public void Move_WhenReachesEnd_ReturnsSuccess()
    {
        var maze = CreateSimpleMaze(end: new Position(3, 2));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var result = session.Move(Direction.East, maze);

        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void Move_WhenDoesNotReachEnd_StateRemainsInProgress()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.East, maze);

        Assert.Equal(SessionState.InProgress, session.State);
    }

    [Fact]
    public void Move_WhenBlocked_DoesNotChangePosition()
    {
        var maze = CreateMazeWithBlockedCell(2, 2, northWall: true);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        session.Move(Direction.North, maze);

        Assert.Equal(new Position(2, 2), session.CurrentPosition);
    }

    [Fact]
    public void Move_WhenOutOfBounds_DoesNotChangePosition()
    {
        var maze = CreateSimpleMaze();
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));

        session.Move(Direction.North, maze);

        Assert.Equal(new Position(0, 0), session.CurrentPosition);
    }
}
