using System.Collections.Concurrent;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Infrastructure;

public class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, MazeSession> _sessions = new();

    public Task<MazeSession?> GetByIdAsync(Guid id)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId)
    {
        var sessions = _sessions.Values.Where(s => s.MazeId == mazeId).ToList();
        return Task.FromResult<IEnumerable<MazeSession>>(sessions);
    }

    public Task<IEnumerable<MazeSession>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<MazeSession>>(_sessions.Values.ToList());
    }

    public Task SaveAsync(MazeSession session)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
