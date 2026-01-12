using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MazeOfHateoas.IntegrationTests;

public class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MetricsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAggregateMetrics_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAggregateMetrics_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/api/metrics");
        var content = await response.Content.ReadFromJsonAsync<AggregateMetricsResponse>();

        Assert.NotNull(content);
        Assert.True(content.ActiveSessions >= 0);
    }

    [Fact]
    public async Task GetMazeMetrics_WithValidMaze_ReturnsOk()
    {
        var createMazeResponse = await _client.PostAsync("/api/mazes", null);
        var mazeResponse = await createMazeResponse.Content.ReadFromJsonAsync<MazeResponse>();

        var response = await _client.GetAsync($"/api/metrics/mazes/{mazeResponse!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMazeMetrics_WithInvalidMaze_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/metrics/mazes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record AggregateMetricsResponse(
        int ActiveSessions,
        int CompletedToday,
        double CompletionRate,
        double AverageMoves,
        Guid? MostActiveMazeId,
        int MostActiveMazeSessionCount,
        double SystemVelocity
    );

    private record MazeResponse(Guid Id, int Width, int Height);
}
