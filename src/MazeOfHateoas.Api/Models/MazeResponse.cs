using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

public class MazeResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("start")]
    public PositionDto Start { get; set; } = new();

    [JsonPropertyName("end")]
    public PositionDto End { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
