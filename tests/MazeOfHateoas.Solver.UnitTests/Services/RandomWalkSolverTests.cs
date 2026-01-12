using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class RandomWalkSolverTests
{
    private readonly Mock<IMazeApiClient> _mockApiClient;
    private readonly Mock<ILogger<RandomWalkSolver>> _mockLogger;
    private readonly IOptions<SolverSettings> _settings;
    private readonly Guid _mazeId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public RandomWalkSolverTests()
    {
        _mockApiClient = new Mock<IMazeApiClient>();
        _mockLogger = new Mock<ILogger<RandomWalkSolver>>();
        _settings = Options.Create(new SolverSettings { DelayBetweenMovesMs = 0 });
    }

    [Fact]
    public async Task SolveAsync_WhenAlreadyAtEnd_ReturnsImmediately()
    {
        var maze = CreateMaze();
        var completedSession = CreateSession(9, 9, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

        var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

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

        var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(1, result.MoveCount);
    }

    [Fact]
    public async Task SolveAsync_WhenDeadEnd_BacktracksToLastJunction()
    {
        var maze = CreateMaze();

        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
            CreateSession(1, 0, "InProgress", ("west", "/move/west")),
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
            CreateSession(0, 1, "Completed")
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(3, result.MoveCount);
    }

    [Fact]
    public async Task SolveAsync_WithSeededRandom_ChoosesRandomlyAmongUnvisited()
    {
        var maze = CreateMaze();
        var seededRandom = new Random(42);

        var chosenDirections = new List<string>();
        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")),
            CreateSession(0, 1, "Completed")
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .Callback<Link, CancellationToken>((link, _) => chosenDirections.Add(link.Href))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new RandomWalkSolver(_mockApiClient.Object, _settings, _mockLogger.Object, seededRandom);

        await solver.SolveAsync(maze);

        // With seed 42, Random.Next(2) returns 1, so index 1 = south
        Assert.Single(chosenDirections);
        Assert.Equal("/move/south", chosenDirections[0]);
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
