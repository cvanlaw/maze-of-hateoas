using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Response model containing a list of mazes.
/// </summary>
public class MazeListResponse
{
    /// <summary>
    /// The list of available mazes.
    /// </summary>
    [JsonPropertyName("mazes")]
    public List<MazeSummaryResponse> Mazes { get; set; } = new();

    /// <summary>
    /// HATEOAS links for collection-level actions.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
