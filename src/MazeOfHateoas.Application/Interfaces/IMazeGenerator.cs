using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.Interfaces;

public interface IMazeGenerator
{
    Maze Generate(int width, int height, Random? random = null);
}
