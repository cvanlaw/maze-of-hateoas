using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.Interfaces;

public interface IMazeRepository
{
    Task<Maze?> GetByIdAsync(Guid id);
    Task<IEnumerable<Maze>> GetAllAsync();
    Task SaveAsync(Maze maze);
}
