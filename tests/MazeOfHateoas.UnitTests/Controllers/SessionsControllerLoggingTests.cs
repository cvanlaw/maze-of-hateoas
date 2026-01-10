using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
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
        var maze = CreateTestMaze();
        await _mazeRepository.SaveAsync(maze);

        await _controller.CreateSession(maze.Id);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Session started", logEntry.Message);
    }

    [Fact]
    public async Task GetSession_WhenSessionNotFound_LogsWarning()
    {
        var maze = CreateTestMaze();
        await _mazeRepository.SaveAsync(maze);

        await _controller.GetSession(maze.Id, Guid.NewGuid());

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Contains("not found", logEntry.Message);
    }

    [Fact]
    public async Task Move_WhenSuccessful_LogsInformation()
    {
        var maze = CreateTestMazeWithOpenPaths();
        await _mazeRepository.SaveAsync(maze);
        var session = new MazeSession(Guid.NewGuid(), maze.Id, maze.Start);
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
        var session = new MazeSession(Guid.NewGuid(), maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "east");

        var logs = _logger.Collector.GetSnapshot();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message.Contains("completed"));
    }

    private static Maze CreateTestMaze()
    {
        var cells = new Cell[5, 5];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), true, true, true, true);
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, new Position(0, 0), new Position(4, 4), DateTime.UtcNow);
    }

    private static Maze CreateTestMazeWithOpenPaths()
    {
        var cells = new Cell[5, 5];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                cells[x, y] = new Cell(new Position(x, y), false, false, false, false);
            }
        }
        return new Maze(Guid.NewGuid(), 5, 5, cells, new Position(0, 0), new Position(4, 4), DateTime.UtcNow);
    }

    private static Maze CreateMazeForCompletion()
    {
        var cells = new Cell[2, 1];
        cells[0, 0] = new Cell(new Position(0, 0), true, true, false, true);
        cells[1, 0] = new Cell(new Position(1, 0), true, true, true, false);
        return new Maze(Guid.NewGuid(), 2, 1, cells, new Position(0, 0), new Position(1, 0), DateTime.UtcNow);
    }

    private class TestMazeRepository : IMazeRepository
    {
        private readonly Dictionary<Guid, Maze> _mazes = new();

        public Task<Maze?> GetByIdAsync(Guid id) =>
            Task.FromResult(_mazes.GetValueOrDefault(id));

        public Task<IEnumerable<Maze>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Maze>>(_mazes.Values);

        public Task SaveAsync(Maze maze)
        {
            _mazes[maze.Id] = maze;
            return Task.CompletedTask;
        }
    }

    private class TestSessionRepository : ISessionRepository
    {
        private readonly Dictionary<Guid, MazeSession> _sessions = new();

        public Task<MazeSession?> GetByIdAsync(Guid id) =>
            Task.FromResult(_sessions.GetValueOrDefault(id));

        public Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId) =>
            Task.FromResult(_sessions.Values.Where(s => s.MazeId == mazeId));

        public Task SaveAsync(MazeSession session)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
