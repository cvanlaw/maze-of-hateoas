using MazeOfHateoas.Solver.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class SolverHostedService : BackgroundService
{
    private readonly IMazeApiClient _apiClient;
    private readonly ISolver _solver;
    private readonly SolverSettings _settings;
    private readonly ILogger<SolverHostedService> _logger;
    private readonly SolverStats _stats = new();

    public SolverHostedService(
        IMazeApiClient apiClient,
        ISolver solver,
        IOptions<SolverSettings> settings,
        ILogger<SolverHostedService> logger)
    {
        _apiClient = apiClient;
        _solver = solver;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Solver starting, connecting to {ApiBaseUrl}", _settings.ApiBaseUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var maze = await _apiClient.CreateMazeAsync(
                    _settings.MazeWidth,
                    _settings.MazeHeight,
                    stoppingToken);

                var result = await _solver.SolveAsync(maze, stoppingToken);
                _stats.Record(result);

                if ((_stats.MazesSolved + _stats.MazesFailed) % _settings.StatsIntervalMazes == 0)
                {
                    LogStats();
                }

                if (_settings.DelayBetweenMazesMs > 0)
                {
                    await Task.Delay(_settings.DelayBetweenMazesMs, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API request failed, retrying after delay");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error, retrying after delay");
                await Task.Delay(5000, stoppingToken);
            }
        }

        LogStats();
        _logger.LogInformation("Solver stopped");
    }

    private void LogStats()
    {
        _logger.LogInformation(
            "Stats: {MazesSolved} solved, {MazesFailed} failed, {TotalMoves} total moves, avg {AvgMoves:F1} moves/maze",
            _stats.MazesSolved,
            _stats.MazesFailed,
            _stats.TotalMoves,
            _stats.AverageMoves);
    }
}
