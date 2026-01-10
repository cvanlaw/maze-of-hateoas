using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Services;

public class SessionLinkGeneratorTests
{
    private readonly SessionLinkGenerator _generator = new();

    private static Link GetLink(Dictionary<string, object> links, string key)
    {
        return (Link)links[key];
    }

    private static Maze CreateMazeWithCell(int width, int height, Cell cell)
    {
        var cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), true, true, true, true);
            }
        }
        cells[cell.Position.X, cell.Position.Y] = cell;
        return new Maze(Guid.NewGuid(), width, height, cells, new Position(0, 0), new Position(width - 1, height - 1), DateTime.UtcNow);
    }

    [Fact]
    public void GenerateLinks_AlwaysIncludesSelfLink()
    {
        var maze = CreateMazeWithCell(5, 5, new Cell(new Position(2, 2), true, true, true, true));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("self"));
        var selfLink = GetLink(links, "self");
        Assert.Contains($"/api/mazes/{session.MazeId}/sessions/{session.Id}", selfLink.Href);
        Assert.Equal("self", selfLink.Rel);
        Assert.Equal("GET", selfLink.Method);
    }

    [Fact]
    public void GenerateLinks_WhenNorthIsOpen_IncludesNorthLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: false, HasSouthWall: true, HasEastWall: true, HasWestWall: true);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("north"));
        var northLink = GetLink(links, "north");
        Assert.Contains("/move/north", northLink.Href);
        Assert.Equal("move", northLink.Rel);
        Assert.Equal("POST", northLink.Method);
    }

    [Fact]
    public void GenerateLinks_WhenNorthIsBlocked_DoesNotIncludeNorthLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: true, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("north"));
    }

    [Fact]
    public void GenerateLinks_WhenAtTopBoundary_DoesNotIncludeNorthLink()
    {
        var cell = new Cell(new Position(2, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 0));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("north"));
    }

    [Fact]
    public void GenerateLinks_WhenSouthIsOpen_IncludesSouthLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: true, HasSouthWall: false, HasEastWall: true, HasWestWall: true);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("south"));
        var southLink = GetLink(links, "south");
        Assert.Contains("/move/south", southLink.Href);
    }

    [Fact]
    public void GenerateLinks_WhenSouthIsBlocked_DoesNotIncludeSouthLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: false, HasSouthWall: true, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("south"));
    }

    [Fact]
    public void GenerateLinks_WhenAtBottomBoundary_DoesNotIncludeSouthLink()
    {
        var cell = new Cell(new Position(2, 4), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 4));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("south"));
    }

    [Fact]
    public void GenerateLinks_WhenEastIsOpen_IncludesEastLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: true, HasSouthWall: true, HasEastWall: false, HasWestWall: true);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("east"));
        var eastLink = GetLink(links, "east");
        Assert.Contains("/move/east", eastLink.Href);
    }

    [Fact]
    public void GenerateLinks_WhenEastIsBlocked_DoesNotIncludeEastLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: false, HasSouthWall: false, HasEastWall: true, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("east"));
    }

    [Fact]
    public void GenerateLinks_WhenAtRightBoundary_DoesNotIncludeEastLink()
    {
        var cell = new Cell(new Position(4, 2), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(4, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("east"));
    }

    [Fact]
    public void GenerateLinks_WhenWestIsOpen_IncludesWestLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: true, HasSouthWall: true, HasEastWall: true, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("west"));
        var westLink = GetLink(links, "west");
        Assert.Contains("/move/west", westLink.Href);
    }

    [Fact]
    public void GenerateLinks_WhenWestIsBlocked_DoesNotIncludeWestLink()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: true);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("west"));
    }

    [Fact]
    public void GenerateLinks_WhenAtLeftBoundary_DoesNotIncludeWestLink()
    {
        var cell = new Cell(new Position(0, 2), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("west"));
    }

    [Fact]
    public void GenerateLinks_AtCorner00_OnlyIncludesAvailableDirections()
    {
        var cell = new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("self"));
        Assert.False(links.ContainsKey("north"), "North blocked by boundary");
        Assert.False(links.ContainsKey("west"), "West blocked by boundary");
        Assert.True(links.ContainsKey("south"), "South should be available");
        Assert.True(links.ContainsKey("east"), "East should be available");
    }

    [Fact]
    public void GenerateLinks_AllDirectionsOpen_IncludesAllMoveLinks()
    {
        var cell = new Cell(new Position(2, 2), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: false);
        var maze = CreateMazeWithCell(5, 5, cell);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(2, 2));

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("self"));
        Assert.True(links.ContainsKey("north"));
        Assert.True(links.ContainsKey("south"));
        Assert.True(links.ContainsKey("east"));
        Assert.True(links.ContainsKey("west"));
        Assert.Equal(5, links.Count);
    }

    private static Maze CreateMazeForCompletion(Position start, Position end)
    {
        var cells = new Cell[5, 5];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), false, false, false, false);
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, start, end, DateTime.UtcNow);
    }

    [Fact]
    public void GenerateLinks_WhenSessionCompleted_IncludesMazesLink()
    {
        var maze = CreateMazeForCompletion(new Position(0, 0), new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));
        session.Move(Direction.East, maze);

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("mazes"));
        var mazesLink = GetLink(links, "mazes");
        Assert.Equal("/api/mazes", mazesLink.Href);
        Assert.Equal("collection", mazesLink.Rel);
        Assert.Equal("GET", mazesLink.Method);
    }

    [Fact]
    public void GenerateLinks_WhenSessionCompleted_IncludesNewMazeLink()
    {
        var maze = CreateMazeForCompletion(new Position(0, 0), new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));
        session.Move(Direction.East, maze);

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("newMaze"));
        var newMazeLink = GetLink(links, "newMaze");
        Assert.Equal("/api/mazes", newMazeLink.Href);
        Assert.Equal("create", newMazeLink.Rel);
        Assert.Equal("POST", newMazeLink.Method);
    }

    [Fact]
    public void GenerateLinks_WhenSessionCompleted_DoesNotIncludeMoveLinks()
    {
        var maze = CreateMazeForCompletion(new Position(0, 0), new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));
        session.Move(Direction.East, maze);

        var links = _generator.GenerateLinks(session, maze);

        Assert.False(links.ContainsKey("north"));
        Assert.False(links.ContainsKey("south"));
        Assert.False(links.ContainsKey("east"));
        Assert.False(links.ContainsKey("west"));
    }

    [Fact]
    public void GenerateLinks_WhenSessionCompleted_StillIncludesSelfLink()
    {
        var maze = CreateMazeForCompletion(new Position(0, 0), new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));
        session.Move(Direction.East, maze);

        var links = _generator.GenerateLinks(session, maze);

        Assert.True(links.ContainsKey("self"));
    }

    [Fact]
    public void GenerateLinks_WhenSessionCompleted_HasExactlyThreeLinks()
    {
        var maze = CreateMazeForCompletion(new Position(0, 0), new Position(1, 0));
        var session = new MazeSession(Guid.NewGuid(), maze.Id, new Position(0, 0));
        session.Move(Direction.East, maze);

        var links = _generator.GenerateLinks(session, maze);

        Assert.Equal(3, links.Count);
    }
}
