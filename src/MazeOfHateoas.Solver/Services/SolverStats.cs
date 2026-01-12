namespace MazeOfHateoas.Solver.Services;

public class SolverStats
{
    public int MazesSolved { get; private set; }
    public int MazesFailed { get; private set; }
    public long TotalMoves { get; private set; }
    public long TotalElapsedMs { get; private set; }

    public double AverageMoves => MazesSolved > 0 ? (double)TotalMoves / MazesSolved : 0;
    public double AverageElapsedMs => MazesSolved > 0 ? (double)TotalElapsedMs / MazesSolved : 0;

    public void Record(SolveResult result)
    {
        if (result.Success)
        {
            MazesSolved++;
            TotalMoves += result.MoveCount;
            TotalElapsedMs += result.ElapsedMs;
        }
        else
        {
            MazesFailed++;
        }
    }
}
