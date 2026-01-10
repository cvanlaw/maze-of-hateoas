using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Response model containing detailed maze information.
/// </summary>
public class MazeResponse
{
    /// <summary>
    /// The unique identifier of the maze.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The width of the maze in cells.
    /// </summary>
    /// <example>10</example>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// The height of the maze in cells.
    /// </summary>
    /// <example>10</example>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// The starting position of the maze (where players begin).
    /// </summary>
    [JsonPropertyName("start")]
    public PositionDto Start { get; set; } = new();

    /// <summary>
    /// The ending position of the maze (the goal).
    /// </summary>
    [JsonPropertyName("end")]
    public PositionDto End { get; set; } = new();

    /// <summary>
    /// The timestamp when the maze was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// HATEOAS links for available actions on this maze.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
