using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class SolverHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateAndSolveMazes()
    {
        var mockApiClient = new Mock<IMazeApiClient>();
        var mockSolver = new Mock<ISolver>();
        var mockLogger = new Mock<ILogger<SolverHostedService>>();
        var settings = Options.Create(new SolverSettings
        {
            MazeWidth = 5,
            MazeHeight = 5,
            DelayBetweenMazesMs = 0,
            StatsIntervalMazes = 100
        });

        var maze = new MazeResponse
        {
            Id = Guid.NewGuid(),
            Width = 5,
            Height = 5,
            Start = new PositionDto { X = 0, Y = 0 },
            End = new PositionDto { X = 4, Y = 4 },
            CreatedAt = DateTime.UtcNow,
            Links = new Dictionary<string, Link>()
        };

        mockApiClient.Setup(c => c.CreateMazeAsync(5, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(maze);
        mockSolver.Setup(s => s.SolveAsync(maze, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolveResult(maze.Id, Guid.NewGuid(), 20, 500, true));

        var service = new SolverHostedService(
            mockApiClient.Object,
            mockSolver.Object,
            settings,
            mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mockApiClient.Verify(c => c.CreateMazeAsync(5, 5, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockSolver.Verify(s => s.SolveAsync(maze, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
