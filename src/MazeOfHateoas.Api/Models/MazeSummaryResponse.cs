using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

public class MazeSummaryResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
