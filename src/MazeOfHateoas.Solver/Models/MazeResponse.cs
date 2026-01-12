using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record MazeResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("start")]
    public required PositionDto Start { get; init; }

    [JsonPropertyName("end")]
    public required PositionDto End { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; init; } = new();
}
