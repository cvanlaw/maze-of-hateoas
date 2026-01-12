using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Helpers;

public class TestMazeRepository : IMazeRepository
{
    private readonly Dictionary<Guid, Maze> _mazes = new();

    public Task<Maze?> GetByIdAsync(Guid id) =>
        Task.FromResult(_mazes.GetValueOrDefault(id));

    public Task<IEnumerable<Maze>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Maze>>(_mazes.Values.ToList());

    public Task SaveAsync(Maze maze)
    {
        _mazes[maze.Id] = maze;
        return Task.CompletedTask;
    }

    public void Add(Maze maze) => _mazes[maze.Id] = maze;
}

public class TestSessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, MazeSession> _sessions = new();

    public Task<MazeSession?> GetByIdAsync(Guid id) =>
        Task.FromResult(_sessions.GetValueOrDefault(id));

    public Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId) =>
        Task.FromResult<IEnumerable<MazeSession>>(
            _sessions.Values.Where(s => s.MazeId == mazeId).ToList());

    public Task<IEnumerable<MazeSession>> GetAllAsync() =>
        Task.FromResult<IEnumerable<MazeSession>>(_sessions.Values.ToList());

    public Task SaveAsync(MazeSession session)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public void Add(MazeSession session) => _sessions[session.Id] = session;
}
