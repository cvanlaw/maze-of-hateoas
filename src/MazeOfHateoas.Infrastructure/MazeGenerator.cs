using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Infrastructure;

public class MazeGenerator : IMazeGenerator
{
    public Maze Generate(int width, int height, Random? random = null)
    {
        random ??= new Random();

        var walls = InitializeWalls(width, height);
        var visited = new bool[width, height];

        CarvePassages(0, 0, width, height, walls, visited, random);

        var cells = BuildCells(width, height, walls);

        return new Maze(
            id: Guid.NewGuid(),
            width: width,
            height: height,
            cells: cells,
            start: new Position(0, 0),
            end: new Position(width - 1, height - 1),
            createdAt: DateTime.UtcNow
        );
    }

    private static (bool[,] horizontal, bool[,] vertical) InitializeWalls(int width, int height)
    {
        var horizontal = new bool[width, height + 1];
        var vertical = new bool[width + 1, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
                horizontal[x, y] = true;
        }

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
                vertical[x, y] = true;
        }

        return (horizontal, vertical);
    }

    private static void CarvePassages(int startX, int startY, int width, int height,
        (bool[,] horizontal, bool[,] vertical) walls, bool[,] visited, Random random)
    {
        var stack = new Stack<(int x, int y)>();
        stack.Push((startX, startY));
        visited[startX, startY] = true;

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            var neighbors = GetUnvisitedNeighbors(x, y, width, height, visited);

            if (neighbors.Count > 0)
            {
                stack.Push((x, y));

                var (nx, ny, direction) = neighbors[random.Next(neighbors.Count)];

                RemoveWall(x, y, direction, walls);

                visited[nx, ny] = true;
                stack.Push((nx, ny));
            }
        }
    }

    private static List<(int x, int y, Direction direction)> GetUnvisitedNeighbors(
        int x, int y, int width, int height, bool[,] visited)
    {
        var neighbors = new List<(int, int, Direction)>();

        if (y > 0 && !visited[x, y - 1])
            neighbors.Add((x, y - 1, Direction.North));
        if (y < height - 1 && !visited[x, y + 1])
            neighbors.Add((x, y + 1, Direction.South));
        if (x < width - 1 && !visited[x + 1, y])
            neighbors.Add((x + 1, y, Direction.East));
        if (x > 0 && !visited[x - 1, y])
            neighbors.Add((x - 1, y, Direction.West));

        return neighbors;
    }

    private static void RemoveWall(int x, int y, Direction direction,
        (bool[,] horizontal, bool[,] vertical) walls)
    {
        switch (direction)
        {
            case Direction.North:
                walls.horizontal[x, y] = false;
                break;
            case Direction.South:
                walls.horizontal[x, y + 1] = false;
                break;
            case Direction.East:
                walls.vertical[x + 1, y] = false;
                break;
            case Direction.West:
                walls.vertical[x, y] = false;
                break;
        }
    }

    private static Cell[,] BuildCells(int width, int height,
        (bool[,] horizontal, bool[,] vertical) walls)
    {
        var cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(
                    Position: new Position(x, y),
                    HasNorthWall: walls.horizontal[x, y],
                    HasSouthWall: walls.horizontal[x, y + 1],
                    HasEastWall: walls.vertical[x + 1, y],
                    HasWestWall: walls.vertical[x, y]
                );
            }
        }

        return cells;
    }
}
