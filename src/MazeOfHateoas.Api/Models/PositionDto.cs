using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

public class PositionDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    public PositionDto() { }

    public PositionDto(int x, int y)
    {
        X = x;
        Y = y;
    }
}
