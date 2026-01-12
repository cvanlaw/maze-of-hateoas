using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.Services;

public interface IMazeApiClient
{
    Task<MazeResponse> CreateMazeAsync(int width, int height, CancellationToken ct = default);
    Task<SessionResponse> StartSessionAsync(Link startLink, CancellationToken ct = default);
    Task<SessionResponse> MoveAsync(Link moveLink, CancellationToken ct = default);
}
