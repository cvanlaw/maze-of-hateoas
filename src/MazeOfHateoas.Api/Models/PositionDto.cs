using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Represents a position in the maze grid.
/// </summary>
public class PositionDto
{
    /// <summary>
    /// The X coordinate (column) in the maze grid. 0 is the leftmost column.
    /// </summary>
    /// <example>5</example>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate (row) in the maze grid. 0 is the topmost row.
    /// </summary>
    /// <example>3</example>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    public PositionDto() { }

    public PositionDto(int x, int y)
    {
        X = x;
        Y = y;
    }
}
