using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Api.Services;

public class SessionLinkGenerator : ISessionLinkGenerator
{
    public Dictionary<string, object> GenerateLinks(MazeSession session, Maze maze)
    {
        var links = new Dictionary<string, object>
        {
            ["self"] = new Link(
                $"/api/mazes/{session.MazeId}/sessions/{session.Id}",
                "self",
                "GET")
        };

        if (session.State == SessionState.Completed)
        {
            links["mazes"] = new Link("/api/mazes", "collection", "GET");
            links["newMaze"] = new Link("/api/mazes", "create", "POST");
            return links;
        }

        var cell = maze.GetCell(session.CurrentPosition.X, session.CurrentPosition.Y);

        if (cell.CanMove(Direction.North) && session.CurrentPosition.Y > 0)
        {
            links["north"] = new Link(
                $"/api/mazes/{session.MazeId}/sessions/{session.Id}/move/north",
                "move",
                "POST");
        }

        if (cell.CanMove(Direction.South) && session.CurrentPosition.Y < maze.Height - 1)
        {
            links["south"] = new Link(
                $"/api/mazes/{session.MazeId}/sessions/{session.Id}/move/south",
                "move",
                "POST");
        }

        if (cell.CanMove(Direction.East) && session.CurrentPosition.X < maze.Width - 1)
        {
            links["east"] = new Link(
                $"/api/mazes/{session.MazeId}/sessions/{session.Id}/move/east",
                "move",
                "POST");
        }

        if (cell.CanMove(Direction.West) && session.CurrentPosition.X > 0)
        {
            links["west"] = new Link(
                $"/api/mazes/{session.MazeId}/sessions/{session.Id}/move/west",
                "move",
                "POST");
        }

        return links;
    }
}
