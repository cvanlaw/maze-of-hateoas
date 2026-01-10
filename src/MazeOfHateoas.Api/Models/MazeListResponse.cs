using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

public class MazeListResponse
{
    [JsonPropertyName("mazes")]
    public List<MazeSummaryResponse> Mazes { get; set; } = new();

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
