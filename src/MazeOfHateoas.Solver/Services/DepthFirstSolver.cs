using System.Diagnostics;
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class DepthFirstSolver : ISolver
{
    private static readonly string[] Directions = ["north", "south", "east", "west"];

    private readonly IMazeApiClient _apiClient;
    private readonly SolverSettings _settings;
    private readonly ILogger<DepthFirstSolver> _logger;

    public DepthFirstSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<DepthFirstSolver> logger)
    {
        _apiClient = apiClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var moveCount = 0;

        var startLink = maze.Links["start"];
        var session = await _apiClient.StartSessionAsync(startLink, ct);

        _logger.LogInformation("Started maze {MazeId}, session {SessionId} at ({X},{Y})",
            maze.Id, session.Id, session.CurrentPosition.X, session.CurrentPosition.Y);

        var visited = new HashSet<(int X, int Y)>();
        var backtrackStack = new Stack<(int X, int Y)>();
        visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));

        while (session.State != "Completed" && !ct.IsCancellationRequested)
        {
            var currentPos = (session.CurrentPosition.X, session.CurrentPosition.Y);
            var availableMoves = GetAvailableMoves(session);
            var unvisitedMoves = availableMoves
                .Where(m => !visited.Contains(GetTargetPosition(currentPos, m.direction)))
                .ToList();

            Link? moveLink;
            string direction;

            if (unvisitedMoves.Count > 0)
            {
                backtrackStack.Push(currentPos);
                (direction, moveLink) = unvisitedMoves[0];
            }
            else if (backtrackStack.Count > 0)
            {
                var backtrackTo = backtrackStack.Pop();
                (direction, moveLink) = GetMoveToward(currentPos, backtrackTo, availableMoves);
            }
            else
            {
                _logger.LogWarning("No moves available and backtrack stack empty at ({X},{Y})",
                    currentPos.X, currentPos.Y);
                break;
            }

            var targetPos = GetTargetPosition(currentPos, direction);
            _logger.LogDebug("Moving {Direction} from ({FromX},{FromY}) to ({ToX},{ToY}), visited: {VisitedCount}",
                direction, currentPos.X, currentPos.Y, targetPos.X, targetPos.Y, visited.Count);

            session = await _apiClient.MoveAsync(moveLink, ct);
            visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));
            moveCount++;

            if (_settings.DelayBetweenMovesMs > 0)
                await Task.Delay(_settings.DelayBetweenMovesMs, ct);
        }

        stopwatch.Stop();
        var success = session.State == "Completed";

        _logger.LogInformation("Maze {MazeId} {Result} in {MoveCount} moves ({ElapsedMs}ms)",
            maze.Id, success ? "solved" : "failed", moveCount, stopwatch.ElapsedMilliseconds);

        return new SolveResult(maze.Id, session.Id, moveCount, stopwatch.ElapsedMilliseconds, success);
    }

    private static List<(string direction, Link link)> GetAvailableMoves(SessionResponse session) =>
        Directions
            .Where(d => session.Links.ContainsKey(d))
            .Select(d => (d, session.Links[d]))
            .ToList();

    private static (int X, int Y) GetTargetPosition((int X, int Y) from, string direction) => direction switch
    {
        "north" => (from.X, from.Y - 1),
        "south" => (from.X, from.Y + 1),
        "east" => (from.X + 1, from.Y),
        "west" => (from.X - 1, from.Y),
        _ => from
    };

    private static (string direction, Link link) GetMoveToward(
        (int X, int Y) from,
        (int X, int Y) target,
        List<(string direction, Link link)> availableMoves)
    {
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;

        string preferredDirection;
        if (dx > 0) preferredDirection = "east";
        else if (dx < 0) preferredDirection = "west";
        else if (dy > 0) preferredDirection = "south";
        else preferredDirection = "north";

        return availableMoves.FirstOrDefault(m => m.direction == preferredDirection);
    }
}
