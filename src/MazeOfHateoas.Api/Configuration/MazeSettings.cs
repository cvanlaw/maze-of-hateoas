namespace MazeOfHateoas.Api.Configuration;

public class MazeSettings
{
    public const string SectionName = "Maze";

    public int DefaultWidth { get; set; } = 10;
    public int DefaultHeight { get; set; } = 10;
}
