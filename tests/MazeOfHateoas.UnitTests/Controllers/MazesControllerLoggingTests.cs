using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Controllers;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.UnitTests.Controllers;

public class MazesControllerLoggingTests
{
    private readonly FakeLogger<MazesController> _logger;
    private readonly MazesController _controller;
    private readonly IMazeRepository _mazeRepository;
    private readonly IMazeGenerator _mazeGenerator;

    public MazesControllerLoggingTests()
    {
        _logger = new FakeLogger<MazesController>();
        _mazeRepository = new TestMazeRepository();
        _mazeGenerator = new TestMazeGenerator();
        var settings = Options.Create(new MazeSettings
        {
            DefaultWidth = 10,
            DefaultHeight = 10,
            MaxWidth = 50,
            MaxHeight = 50
        });
        _controller = new MazesController(_mazeGenerator, _mazeRepository, settings, _logger);
    }

    [Fact]
    public async Task CreateMaze_LogsInformationWithMazeDetails()
    {
        var request = new CreateMazeRequest { Width = 5, Height = 5 };

        await _controller.CreateMaze(request);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("Maze created", logEntry.Message);
    }

    [Fact]
    public async Task GetMaze_WhenNotFound_LogsWarning()
    {
        var nonExistentId = Guid.NewGuid();

        await _controller.GetMaze(nonExistentId);

        var logEntry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, logEntry.Level);
        Assert.Contains("not found", logEntry.Message);
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

    private class TestMazeGenerator : IMazeGenerator
    {
        public Maze Generate(int width, int height, Random? random = null)
        {
            var cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new Cell(new Position(x, y), true, true, true, true);
                }
            }
            return new Maze(Guid.NewGuid(), width, height, cells,
                new Position(0, 0), new Position(width - 1, height - 1), DateTime.UtcNow);
        }
    }
}
