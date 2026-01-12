using MazeOfHateoas.Solver.Services;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class SolverStatsTests
{
    [Fact]
    public void Record_ShouldUpdateTotals()
    {
        var stats = new SolverStats();

        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 50, 1000, true));
        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 30, 500, true));

        Assert.Equal(2, stats.MazesSolved);
        Assert.Equal(80, stats.TotalMoves);
        Assert.Equal(40.0, stats.AverageMoves);
    }

    [Fact]
    public void Record_ShouldTrackFailures()
    {
        var stats = new SolverStats();

        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 10, 100, true));
        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 5, 50, false));

        Assert.Equal(1, stats.MazesSolved);
        Assert.Equal(1, stats.MazesFailed);
    }
}
