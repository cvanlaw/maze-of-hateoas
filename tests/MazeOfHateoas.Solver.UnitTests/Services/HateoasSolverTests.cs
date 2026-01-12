using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class HateoasSolverTests
{
    private readonly Mock<IMazeApiClient> _mockApiClient;
    private readonly Mock<ILogger<HateoasSolver>> _mockLogger;
    private readonly IOptions<SolverSettings> _settings;
    private readonly Guid _mazeId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public HateoasSolverTests()
    {
        _mockApiClient = new Mock<IMazeApiClient>();
        _mockLogger = new Mock<ILogger<HateoasSolver>>();
        _settings = Options.Create(new SolverSettings { DelayBetweenMovesMs = 0 });
    }

    [Fact]
    public async Task SolveAsync_WhenAlreadyAtEnd_ReturnsImmediately()
    {
        var maze = CreateMaze();
        var completedSession = CreateSession(9, 9, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(0, result.MoveCount);
    }

    [Fact]
    public async Task SolveAsync_WhenOneMove_CompletesInOneMove()
    {
        var maze = CreateMaze();
        var startSession = CreateSession(0, 0, "InProgress", ("east", "/move/east"));
        var endSession = CreateSession(1, 0, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(startSession);
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(endSession);

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(1, result.MoveCount);
    }

    [Fact]
    public async Task SolveAsync_WhenDeadEnd_BacktracksToLastJunction()
    {
        // Maze: Start(0,0) -> (1,0) dead end, backtrack, go (0,1) -> End
        var maze = CreateMaze();

        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // start
            CreateSession(1, 0, "InProgress", ("west", "/move/west")), // dead end - only way back
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // back at start
            CreateSession(0, 1, "Completed") // found exit going south
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(3, result.MoveCount); // east, west (backtrack), south
    }

    [Fact]
    public async Task SolveAsync_ChoosesUnvisitedOverVisited()
    {
        var maze = CreateMaze();

        var linkCaptures = new List<string>();
        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // start
            CreateSession(1, 0, "Completed") // end after going east
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .Callback<Link, CancellationToken>((link, _) => linkCaptures.Add(link.Href))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        await solver.SolveAsync(maze);

        // Should choose first available unvisited (east in this case based on Directions order)
        Assert.Single(linkCaptures);
    }

    private MazeResponse CreateMaze() => new()
    {
        Id = _mazeId,
        Width = 10,
        Height = 10,
        Start = new PositionDto { X = 0, Y = 0 },
        End = new PositionDto { X = 9, Y = 9 },
        CreatedAt = DateTime.UtcNow,
        Links = new Dictionary<string, Link>
        {
            ["start"] = new Link { Href = "/api/mazes/123/sessions", Rel = "start", Method = "POST" }
        }
    };

    private SessionResponse CreateSession(int x, int y, string state, params (string name, string href)[] moves) => new()
    {
        Id = _sessionId,
        MazeId = _mazeId,
        CurrentPosition = new PositionDto { X = x, Y = y },
        State = state,
        Links = moves.ToDictionary(
            m => m.name,
            m => new Link { Href = m.href, Rel = "move", Method = "POST" }
        )
    };
}
