using MazeOfHateoas.Domain;

namespace MazeOfHateoas.Application.Interfaces;

public interface ISessionRepository
{
    Task<MazeSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<MazeSession>> GetByMazeIdAsync(Guid mazeId);
    Task<IEnumerable<MazeSession>> GetAllAsync();
    Task SaveAsync(MazeSession session);
}
