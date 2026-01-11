using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Domain;
using MazeOfHateoas.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace MazeOfHateoas.UnitTests.Controllers;

public class SessionsControllerLoggingTests
{
    private readonly FakeLogger<SessionsController> _logger;
    private readonly SessionsController _controller;
    private readonly TestMazeRepository _mazeRepository;
    private readonly TestSessionRepository _sessionRepository;

    public SessionsControllerLoggingTests()
    {
        _logger = new FakeLogger<SessionsController>();
        _mazeRepository = new TestMazeRepository();
        _sessionRepository = new TestSessionRepository();
        var linkGenerator = new SessionLinkGenerator();
        _controller = new SessionsController(_mazeRepository, _sessionRepository, linkGenerator, _logger);
    }

    [Fact]
    public async Task CreateSession_LogsInformationWithSessionDetails()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: false);
        await _mazeRepository.SaveAsync(maze);

        await _controller.CreateSession(maze.Id);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Session started", logEntry.Message);
    }

    [Fact]
    public async Task GetSession_WhenSessionNotFound_LogsWarning()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: false);
        await _mazeRepository.SaveAsync(maze);

        await _controller.GetSession(maze.Id, Guid.NewGuid());

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Contains("not found", logEntry.Message);
    }

    [Fact]
    public async Task Move_WhenSuccessful_LogsInformation()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: true);
        await _mazeRepository.SaveAsync(maze);
        var session = TestDataBuilders.CreateTestSession(maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "south");

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Move", logEntry.Message);
    }

    [Fact]
    public async Task Move_WhenMazeCompleted_LogsCompletion()
    {
        var maze = CreateMazeForCompletion();
        await _mazeRepository.SaveAsync(maze);
        var session = TestDataBuilders.CreateTestSession(maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "east");

        var logs = _logger.Collector.GetSnapshot();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message.Contains("completed"));
    }

    private static Maze CreateMazeForCompletion()
    {
        var cells = new Cell[2, 1];
        cells[0, 0] = new Cell(new Position(0, 0), true, true, false, true);
        cells[1, 0] = new Cell(new Position(1, 0), true, true, true, false);
        return new Maze(Guid.NewGuid(), 2, 1, cells, new Position(0, 0), new Position(1, 0), DateTime.UtcNow);
    }
}
