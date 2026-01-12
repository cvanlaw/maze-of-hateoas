using MazeOfHateoas.Application.DTOs;

namespace MazeOfHateoas.Application.Interfaces;

public interface IMetricsService
{
    Task<AggregateMetrics> GetAggregateMetricsAsync();
    Task<MazeMetrics?> GetMazeMetricsAsync(Guid mazeId);
}
