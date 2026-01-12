using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record PositionDto
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}
