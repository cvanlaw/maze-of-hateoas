using System.Net.Http.Json;
using System.Text.Json;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;

namespace MazeOfHateoas.Solver.Services;

public class MazeApiClient : IMazeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MazeApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MazeApiClient(HttpClient httpClient, ILogger<MazeApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MazeResponse> CreateMazeAsync(int width, int height, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating maze {Width}x{Height}", width, height);

        var response = await _httpClient.PostAsJsonAsync("/api/mazes", new { width, height }, ct);
        response.EnsureSuccessStatusCode();

        var maze = await response.Content.ReadFromJsonAsync<MazeResponse>(JsonOptions, ct);
        return maze ?? throw new InvalidOperationException("Failed to deserialize maze response");
    }

    public async Task<SessionResponse> StartSessionAsync(Link startLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Starting session via {Href}", startLink.Href);

        var response = await _httpClient.PostAsync(startLink.Href, null, ct);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, ct);
        return session ?? throw new InvalidOperationException("Failed to deserialize session response");
    }

    public async Task<SessionResponse> MoveAsync(Link moveLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Moving via {Href}", moveLink.Href);

        var response = await _httpClient.PostAsync(moveLink.Href, null, ct);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, ct);
        return session ?? throw new InvalidOperationException("Failed to deserialize session response");
    }
}
