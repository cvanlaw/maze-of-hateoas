using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record SessionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("mazeId")]
    public Guid MazeId { get; init; }

    [JsonPropertyName("currentPosition")]
    public required PositionDto CurrentPosition { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("moveResult")]
    public string? MoveResult { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; init; } = new();
}
