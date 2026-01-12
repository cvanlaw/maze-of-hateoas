using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using MazeOfHateoas.Infrastructure;
using MazeOfHateoas.UnitTests.Helpers;

namespace MazeOfHateoas.UnitTests.Infrastructure;

public class MetricsServiceTests
{
    [Fact]
    public async Task GetAggregateMetricsAsync_WithNoSessions_ReturnsZeroCounts()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(0, metrics.ActiveSessions);
        Assert.Equal(0, metrics.CompletedToday);
        Assert.Equal(0, metrics.CompletionRate);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_CountsActiveSessions()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze = TestDataBuilders.CreateTestMaze();
        var session1 = TestDataBuilders.CreateTestSession(maze.Id);
        var session2 = TestDataBuilders.CreateTestSession(maze.Id);
        await mazeRepo.SaveAsync(maze);
        await sessionRepo.SaveAsync(session1);
        await sessionRepo.SaveAsync(session2);
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(2, metrics.ActiveSessions);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_CalculatesCompletionRate()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
        var completedSession = TestDataBuilders.CreateTestSession(maze.Id);
        var activeSession = TestDataBuilders.CreateTestSession(maze.Id);
        await mazeRepo.SaveAsync(maze);

        completedSession.Move(Direction.East, maze);
        completedSession.Move(Direction.East, maze);
        completedSession.Move(Direction.South, maze);
        completedSession.Move(Direction.South, maze);

        await sessionRepo.SaveAsync(completedSession);
        await sessionRepo.SaveAsync(activeSession);
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(50, metrics.CompletionRate);
        Assert.Equal(1, metrics.CompletedToday);
        Assert.Equal(1, metrics.ActiveSessions);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_CalculatesAverageMoves()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
        await mazeRepo.SaveAsync(maze);

        var session1 = TestDataBuilders.CreateTestSession(maze.Id);
        session1.Move(Direction.East, maze);
        session1.Move(Direction.East, maze);
        session1.Move(Direction.South, maze);
        session1.Move(Direction.South, maze);

        var session2 = TestDataBuilders.CreateTestSession(maze.Id);
        session2.Move(Direction.South, maze);
        session2.Move(Direction.South, maze);
        session2.Move(Direction.East, maze);
        session2.Move(Direction.East, maze);

        await sessionRepo.SaveAsync(session1);
        await sessionRepo.SaveAsync(session2);
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(4, metrics.AverageMoves);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_SelectsMostActiveMaze()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze1 = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
        var maze2 = TestDataBuilders.CreateTestMaze(3, 3, allCellsOpen: true);
        await mazeRepo.SaveAsync(maze1);
        await mazeRepo.SaveAsync(maze2);

        await sessionRepo.SaveAsync(TestDataBuilders.CreateTestSession(maze1.Id));
        await sessionRepo.SaveAsync(TestDataBuilders.CreateTestSession(maze1.Id));
        await sessionRepo.SaveAsync(TestDataBuilders.CreateTestSession(maze1.Id));
        await sessionRepo.SaveAsync(TestDataBuilders.CreateTestSession(maze2.Id));

        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetAggregateMetricsAsync();

        Assert.Equal(maze1.Id, metrics.MostActiveMazeId);
        Assert.Equal(3, metrics.MostActiveMazeSessionCount);
    }

    [Fact]
    public async Task GetMazeMetricsAsync_WithValidMaze_ReturnsMetrics()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var maze = TestDataBuilders.CreateTestMaze(5, 5);
        var session = TestDataBuilders.CreateTestSession(maze.Id);
        await mazeRepo.SaveAsync(maze);
        await sessionRepo.SaveAsync(session);
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetMazeMetricsAsync(maze.Id);

        Assert.NotNull(metrics);
        Assert.Equal(maze.Id, metrics.MazeId);
        Assert.Equal(5, metrics.Width);
        Assert.Equal(5, metrics.Height);
        Assert.Equal(1, metrics.ActiveSessions);
        Assert.Single(metrics.Sessions);
    }

    [Fact]
    public async Task GetMazeMetricsAsync_WithInvalidMaze_ReturnsNull()
    {
        var mazeRepo = new TestMazeRepository();
        var sessionRepo = new TestSessionRepository();
        var service = new MetricsService(mazeRepo, sessionRepo);

        var metrics = await service.GetMazeMetricsAsync(Guid.NewGuid());

        Assert.Null(metrics);
    }
}
