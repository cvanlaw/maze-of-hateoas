namespace MazeOfHateoas.Solver.Configuration;

public class SolverSettings
{
    public const string SectionName = "Solver";

    public string ApiBaseUrl { get; set; } = "http://localhost:8080";
    public int MazeWidth { get; set; } = 10;
    public int MazeHeight { get; set; } = 10;
    public int DelayBetweenMazesMs { get; set; } = 2000;
    public int DelayBetweenMovesMs { get; set; } = 0;
    public int StatsIntervalMazes { get; set; } = 10;
    public string Algorithm { get; set; } = "dfs";
}
