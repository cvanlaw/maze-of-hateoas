using MazeOfHateoas.Application.DTOs;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Infrastructure;

public class MetricsService : IMetricsService
{
    private readonly IMazeRepository _mazeRepository;
    private readonly ISessionRepository _sessionRepository;

    public MetricsService(IMazeRepository mazeRepository, ISessionRepository sessionRepository)
    {
        _mazeRepository = mazeRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AggregateMetrics> GetAggregateMetricsAsync()
    {
        var sessions = (await _sessionRepository.GetAllAsync()).ToList();
        var activeSessions = sessions.Count(s => s.State == SessionState.InProgress);
        var completedToday = sessions.Count(s =>
            s.State == SessionState.Completed &&
            s.StartedAt.Date == DateTime.UtcNow.Date);
        var totalCompleted = sessions.Count(s => s.State == SessionState.Completed);
        var completionRate = sessions.Count > 0
            ? (double)totalCompleted / sessions.Count * 100
            : 0;
        var averageMoves = totalCompleted > 0
            ? sessions.Where(s => s.State == SessionState.Completed).Average(s => s.MoveCount)
            : 0;

        var mostActiveMaze = sessions
            .Where(s => s.State == SessionState.InProgress)
            .GroupBy(s => s.MazeId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var sessionCountsByMaze = sessions
            .Where(s => s.State == SessionState.InProgress)
            .GroupBy(s => s.MazeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var systemVelocity = sessions
            .Where(s => s.State == SessionState.InProgress)
            .Sum(s => CalculateVelocity(s));

        return new AggregateMetrics(
            activeSessions,
            completedToday,
            Math.Round(completionRate, 1),
            Math.Round(averageMoves, 1),
            mostActiveMaze?.Key,
            mostActiveMaze?.Count() ?? 0,
            Math.Round(systemVelocity, 1),
            sessionCountsByMaze
        );
    }

    public async Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId)
    {
        var maze = await _mazeRepository.GetByIdAsync(mazeId);
        if (maze == null) return null;

        var sessions = (await _sessionRepository.GetByMazeIdAsync(mazeId)).ToList();
        var activeSessions = sessions.Where(s => s.State == SessionState.InProgress).ToList();
        var completedCount = sessions.Count(s => s.State == SessionState.Completed);

        var totalCells = maze.Width * maze.Height;

        var snapshots = activeSessions.Select(s => new SessionSnapshot(
            s.Id,
            s.CurrentPosition,
            s.MoveCount,
            s.VisitedCells.Count,
            Math.Round((double)s.VisitedCells.Count / totalCells * 100, 1),
            Math.Round(CalculateVelocity(s), 1),
            DateTime.UtcNow - s.StartedAt
        )).ToList();

        return new MazeMetrics(
            maze.Id,
            maze.Width,
            maze.Height,
            ConvertToJaggedArray(maze.Cells, maze.Width, maze.Height),
            activeSessions.Count,
            completedCount,
            snapshots
        );
    }

    private static Cell[][] ConvertToJaggedArray(Cell[,] cells, int width, int height)
    {
        var result = new Cell[height][];
        for (int y = 0; y < height; y++)
        {
            result[y] = new Cell[width];
            for (int x = 0; x < width; x++)
            {
                result[y][x] = cells[x, y];
            }
        }
        return result;
    }

    private static double CalculateVelocity(MazeSession session)
    {
        var duration = DateTime.UtcNow - session.StartedAt;
        return duration.TotalMinutes > 0
            ? session.MoveCount / duration.TotalMinutes
            : 0;
    }
}
