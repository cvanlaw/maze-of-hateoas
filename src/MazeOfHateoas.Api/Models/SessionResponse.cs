using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Response model for a maze navigation session.
/// </summary>
public class SessionResponse
{
    /// <summary>
    /// The unique identifier of the session.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440001</example>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the maze this session belongs to.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [JsonPropertyName("mazeId")]
    public Guid MazeId { get; set; }

    /// <summary>
    /// The current position of the player in the maze.
    /// </summary>
    [JsonPropertyName("currentPosition")]
    public PositionDto CurrentPosition { get; set; } = new();

    /// <summary>
    /// The current state of the session (InProgress or Completed).
    /// </summary>
    /// <example>InProgress</example>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the session was started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// The result of the last move attempt (Success, Blocked, OutOfBounds, AlreadyCompleted).
    /// Only present after a move operation.
    /// </summary>
    /// <example>Success</example>
    [JsonPropertyName("moveResult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MoveResult { get; set; }

    /// <summary>
    /// A message for the player, such as a congratulations message upon completion.
    /// </summary>
    /// <example>Congratulations! You've completed the maze!</example>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// HATEOAS links for available actions. Movement links are only present
    /// for directions that are not blocked by walls.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; set; } = new();
}
