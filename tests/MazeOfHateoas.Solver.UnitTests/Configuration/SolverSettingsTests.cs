using MazeOfHateoas.Solver.Configuration;

namespace MazeOfHateoas.Solver.UnitTests.Configuration;

public class SolverSettingsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var settings = new SolverSettings();

        Assert.Equal("http://localhost:8080", settings.ApiBaseUrl);
        Assert.Equal(10, settings.MazeWidth);
        Assert.Equal(10, settings.MazeHeight);
        Assert.Equal(2000, settings.DelayBetweenMazesMs);
        Assert.Equal(0, settings.DelayBetweenMovesMs);
        Assert.Equal(10, settings.StatsIntervalMazes);
    }
}
