using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class MazeResponseTests
{
    [Fact]
    public void Deserialize_ShouldParseJsonWithLinks()
    {
        var json = """
        {
            "id": "550e8400-e29b-41d4-a716-446655440000",
            "width": 10,
            "height": 10,
            "start": {"x": 0, "y": 0},
            "end": {"x": 9, "y": 9},
            "createdAt": "2026-01-12T10:30:00Z",
            "_links": {
                "self": {"href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET"},
                "start": {"href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST"}
            }
        }
        """;

        var maze = JsonSerializer.Deserialize<MazeResponse>(json);

        Assert.NotNull(maze);
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), maze.Id);
        Assert.Equal(10, maze.Width);
        Assert.Equal(10, maze.Height);
        Assert.Equal(0, maze.Start.X);
        Assert.Equal(9, maze.End.Y);
        Assert.True(maze.Links.ContainsKey("start"));
        Assert.Equal("/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", maze.Links["start"].Href);
    }
}
