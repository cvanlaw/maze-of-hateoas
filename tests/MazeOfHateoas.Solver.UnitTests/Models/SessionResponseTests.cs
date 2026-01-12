using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class SessionResponseTests
{
    [Fact]
    public void Deserialize_ShouldParseInProgressSession()
    {
        var json = """
        {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "mazeId": "550e8400-e29b-41d4-a716-446655440000",
            "currentPosition": {"x": 2, "y": 3},
            "state": "InProgress",
            "_links": {
                "self": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4", "rel": "self", "method": "GET"},
                "north": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4/move/north", "rel": "move", "method": "POST"},
                "east": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4/move/east", "rel": "move", "method": "POST"}
            }
        }
        """;

        var session = JsonSerializer.Deserialize<SessionResponse>(json);

        Assert.NotNull(session);
        Assert.Equal(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), session.Id);
        Assert.Equal("InProgress", session.State);
        Assert.Equal(2, session.CurrentPosition.X);
        Assert.Equal(3, session.CurrentPosition.Y);
        Assert.True(session.Links.ContainsKey("north"));
        Assert.True(session.Links.ContainsKey("east"));
        Assert.False(session.Links.ContainsKey("south"));
        Assert.False(session.Links.ContainsKey("west"));
    }

    [Fact]
    public void Deserialize_ShouldParseCompletedSession()
    {
        var json = """
        {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "mazeId": "550e8400-e29b-41d4-a716-446655440000",
            "currentPosition": {"x": 9, "y": 9},
            "state": "Completed",
            "message": "Congratulations! You've completed the maze!",
            "_links": {
                "self": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4", "rel": "self", "method": "GET"}
            }
        }
        """;

        var session = JsonSerializer.Deserialize<SessionResponse>(json);

        Assert.NotNull(session);
        Assert.Equal("Completed", session.State);
        Assert.Equal("Congratulations! You've completed the maze!", session.Message);
    }
}
