using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Hubs;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Domain;
using MazeOfHateoas.UnitTests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace MazeOfHateoas.UnitTests.Controllers;

public class SessionsControllerSignalRTests
{
    private readonly FakeLogger<SessionsController> _logger;
    private readonly TestMazeRepository _mazeRepository;
    private readonly TestSessionRepository _sessionRepository;
    private readonly Mock<IHubContext<MetricsHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _allGroupProxyMock;
    private readonly Mock<IClientProxy> _mazeGroupProxyMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly SessionsController _controller;

    public SessionsControllerSignalRTests()
    {
        _logger = new FakeLogger<SessionsController>();
        _mazeRepository = new TestMazeRepository();
        _sessionRepository = new TestSessionRepository();
        var linkGenerator = new SessionLinkGenerator();

        _allGroupProxyMock = new Mock<IClientProxy>();
        _mazeGroupProxyMock = new Mock<IClientProxy>();
        _clientsMock = new Mock<IHubClients>();
        _clientsMock.Setup(c => c.Group("all")).Returns(_allGroupProxyMock.Object);
        _clientsMock.Setup(c => c.Group(It.Is<string>(s => s.StartsWith("maze:")))).Returns(_mazeGroupProxyMock.Object);

        _hubContextMock = new Mock<IHubContext<MetricsHub>>();
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _controller = new SessionsController(
            _mazeRepository,
            _sessionRepository,
            linkGenerator,
            _logger,
            _hubContextMock.Object);
    }

    [Fact]
    public async Task CreateSession_BroadcastsSessionStartedEvent_ToAllGroup()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: true);
        await _mazeRepository.SaveAsync(maze);

        await _controller.CreateSession(maze.Id);

        _allGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                "SessionStarted",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task CreateSession_BroadcastsSessionStartedEvent_ToMazeGroup()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: true);
        await _mazeRepository.SaveAsync(maze);

        await _controller.CreateSession(maze.Id);

        _mazeGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                "SessionStarted",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Move_WhenSuccessful_BroadcastsSessionMovedEvent()
    {
        var maze = TestDataBuilders.CreateTestMaze(5, 5, allCellsOpen: true);
        await _mazeRepository.SaveAsync(maze);
        var session = TestDataBuilders.CreateTestSession(maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "south");

        _allGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                "SessionMoved",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Move_WhenMazeCompleted_BroadcastsSessionCompletedEvent()
    {
        var maze = CreateMazeForCompletion();
        await _mazeRepository.SaveAsync(maze);
        var session = TestDataBuilders.CreateTestSession(maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "east");

        _allGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                "SessionCompleted",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Move_WhenBlocked_DoesNotBroadcast()
    {
        var maze = CreateMazeWithBlockedSouth();
        await _mazeRepository.SaveAsync(maze);
        var session = TestDataBuilders.CreateTestSession(maze.Id, maze.Start);
        await _sessionRepository.SaveAsync(session);

        await _controller.Move(maze.Id, session.Id, "south");

        _allGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default),
            Times.Never);
    }

    [Fact]
    public async Task CreateSession_WhenMazeNotFound_DoesNotBroadcast()
    {
        await _controller.CreateSession(Guid.NewGuid());

        _allGroupProxyMock.Verify(
            c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default),
            Times.Never);
    }

    private static Maze CreateMazeForCompletion()
    {
        var cells = new Cell[2, 1];
        cells[0, 0] = new Cell(new Position(0, 0), true, true, false, true);
        cells[1, 0] = new Cell(new Position(1, 0), true, true, true, false);
        return new Maze(Guid.NewGuid(), 2, 1, cells, new Position(0, 0), new Position(1, 0), DateTime.UtcNow);
    }

    private static Maze CreateMazeWithBlockedSouth()
    {
        var cells = new Cell[2, 2];
        cells[0, 0] = new Cell(new Position(0, 0), true, true, false, true);
        cells[1, 0] = new Cell(new Position(1, 0), true, true, true, false);
        cells[0, 1] = new Cell(new Position(0, 1), true, true, false, true);
        cells[1, 1] = new Cell(new Position(1, 1), true, true, true, false);
        return new Maze(Guid.NewGuid(), 2, 2, cells, new Position(0, 0), new Position(1, 1), DateTime.UtcNow);
    }
}
