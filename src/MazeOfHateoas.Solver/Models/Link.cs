using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record Link
{
    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("rel")]
    public required string Rel { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }
}
