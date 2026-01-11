namespace MazeOfHateoas.Application.Services;

public interface IMazeLinkGenerator
{
    Dictionary<string, object> GenerateMazeLinks(Guid mazeId);
    Dictionary<string, object> GenerateListLinks();
}
