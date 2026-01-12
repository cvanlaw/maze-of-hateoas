using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.Services;

public interface ISolver
{
    Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default);
}
