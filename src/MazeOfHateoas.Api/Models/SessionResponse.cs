using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

public class SessionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("mazeId")]
    public Guid MazeId { get; set; }

    [JsonPropertyName("currentPosition")]
    public PositionDto CurrentPosition { get; set; } = new();

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("moveResult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MoveResult { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
