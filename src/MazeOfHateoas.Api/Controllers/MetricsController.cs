using MazeOfHateoas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MazeOfHateoas.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;

    public MetricsController(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAggregateMetrics()
    {
        var metrics = await _metricsService.GetAggregateMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("mazes/{mazeId}")]
    public async Task<IActionResult> GetMazeMetrics(Guid mazeId)
    {
        var metrics = await _metricsService.GetMazeMetricsAsync(mazeId);
        if (metrics == null)
            return NotFound();
        return Ok(metrics);
    }
}
