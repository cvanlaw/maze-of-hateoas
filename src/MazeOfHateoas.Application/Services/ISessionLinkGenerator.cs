using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.Services;

public interface ISessionLinkGenerator
{
    Dictionary<string, object> GenerateLinks(MazeSession session, Maze maze);
}
