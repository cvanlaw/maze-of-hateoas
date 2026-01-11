using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Services;

namespace MazeOfHateoas.Api.Services;

public class MazeLinkGenerator : IMazeLinkGenerator
{
    public Dictionary<string, object> GenerateMazeLinks(Guid mazeId) => new()
    {
        ["self"] = new Link($"/api/mazes/{mazeId}", "self", "GET"),
        ["start"] = new Link($"/api/mazes/{mazeId}/sessions", "start", "POST")
    };

    public Dictionary<string, object> GenerateListLinks() => new()
    {
        ["self"] = new Link("/api/mazes", "self", "GET"),
        ["create"] = new Link("/api/mazes", "create", "POST")
    };
}
