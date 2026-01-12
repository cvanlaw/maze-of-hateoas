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
            Math.Round(systemVelocity, 1)
        );
    }

    public Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId)
    {
        throw new NotImplementedException();
    }

    private static double CalculateVelocity(MazeSession session)
    {
        var duration = DateTime.UtcNow - session.StartedAt;
        return duration.TotalMinutes > 0
            ? session.MoveCount / duration.TotalMinutes
            : 0;
    }
}
