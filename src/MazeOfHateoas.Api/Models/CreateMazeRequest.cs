namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Request model for creating a new maze.
/// </summary>
public class CreateMazeRequest
{
    /// <summary>
    /// The width of the maze in cells. If not specified, uses the configured default.
    /// </summary>
    /// <example>10</example>
    public int? Width { get; set; }

    /// <summary>
    /// The height of the maze in cells. If not specified, uses the configured default.
    /// </summary>
    /// <example>10</example>
    public int? Height { get; set; }
}
