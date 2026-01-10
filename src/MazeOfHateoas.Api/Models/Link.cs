using System.Text.Json.Serialization;

namespace MazeOfHateoas.Api.Models;

/// <summary>
/// Represents a HATEOAS hypermedia link.
/// </summary>
public class Link
{
    /// <summary>
    /// The URL of the linked resource or action.
    /// </summary>
    /// <example>/api/mazes/550e8400-e29b-41d4-a716-446655440000</example>
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// The relationship type of the link (e.g., "self", "start", "move").
    /// </summary>
    /// <example>self</example>
    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP method to use when following this link.
    /// </summary>
    /// <example>GET</example>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    public Link() { }

    public Link(string href, string rel, string method)
    {
        Href = href;
        Rel = rel;
        Method = method;
    }
}
