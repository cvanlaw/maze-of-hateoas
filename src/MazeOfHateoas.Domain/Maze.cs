namespace MazeOfHateoas.Domain;

public class Maze
{
    public Guid Id { get; }
    public int Width { get; }
    public int Height { get; }
    public Cell[,] Cells { get; }
    public Position Start { get; }
    public Position End { get; }
    public DateTime CreatedAt { get; }

    public Maze(Guid id, int width, int height, Cell[,] cells, Position start, Position end, DateTime createdAt)
    {
        Id = id;
        Width = width;
        Height = height;
        Cells = cells;
        Start = start;
        End = end;
        CreatedAt = createdAt;
    }

    public Cell GetCell(int x, int y) => Cells[x, y];
}
