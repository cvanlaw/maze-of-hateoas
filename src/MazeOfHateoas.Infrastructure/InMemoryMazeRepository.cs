using System.Collections.Concurrent;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Infrastructure;

public class InMemoryMazeRepository : IMazeRepository
{
    private readonly ConcurrentDictionary<Guid, Maze> _mazes = new();

    public Task<Maze?> GetByIdAsync(Guid id)
    {
        _mazes.TryGetValue(id, out var maze);
        return Task.FromResult(maze);
    }

    public Task<IEnumerable<Maze>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Maze>>(_mazes.Values.ToList());
    }

    public Task SaveAsync(Maze maze)
    {
        _mazes[maze.Id] = maze;
        return Task.CompletedTask;
    }
}
