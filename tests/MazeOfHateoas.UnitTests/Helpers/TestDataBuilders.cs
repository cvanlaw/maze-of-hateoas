using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Helpers;

public static class TestDataBuilders
{
    public static Maze CreateTestMaze(
        int width = 3,
        int height = 3,
        bool allCellsOpen = true,
        Guid? id = null)
    {
        var cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = allCellsOpen
                    ? new Cell(new Position(x, y), false, false, false, false)
                    : new Cell(new Position(x, y), true, true, true, true);
            }
        }

        return new Maze(
            id ?? Guid.NewGuid(),
            width,
            height,
            cells,
            new Position(0, 0),
            new Position(width - 1, height - 1),
            DateTime.UtcNow);
    }

    public static MazeSession CreateTestSession(
        Guid mazeId,
        Position? position = null,
        Guid? id = null)
    {
        return new MazeSession(
            id ?? Guid.NewGuid(),
            mazeId,
            position ?? new Position(0, 0));
    }
}
