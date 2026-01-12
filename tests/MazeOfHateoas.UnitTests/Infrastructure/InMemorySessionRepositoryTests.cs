using MazeOfHateoas.Domain;
using MazeOfHateoas.Infrastructure;

namespace MazeOfHateoas.UnitTests.Infrastructure;

public class InMemorySessionRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        var repository = new InMemorySessionRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsSession()
    {
        var repository = new InMemorySessionRepository();
        var session = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));

        await repository.SaveAsync(session);
        var result = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
    }

    [Fact]
    public async Task GetByMazeIdAsync_WhenNoSessions_ReturnsEmptyCollection()
    {
        var repository = new InMemorySessionRepository();

        var result = await repository.GetByMazeIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByMazeIdAsync_ReturnsSessionsForMaze()
    {
        var repository = new InMemorySessionRepository();
        var mazeId = Guid.NewGuid();
        var session1 = new MazeSession(Guid.NewGuid(), mazeId, new Position(0, 0));
        var session2 = new MazeSession(Guid.NewGuid(), mazeId, new Position(0, 0));
        var otherMazeSession = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));

        await repository.SaveAsync(session1);
        await repository.SaveAsync(session2);
        await repository.SaveAsync(otherMazeSession);

        var result = await repository.GetByMazeIdAsync(mazeId);

        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Id == session1.Id);
        Assert.Contains(result, s => s.Id == session2.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSessions()
    {
        var repository = new InMemorySessionRepository();
        var session1 = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));
        var session2 = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));
        await repository.SaveAsync(session1);
        await repository.SaveAsync(session2);

        var all = await repository.GetAllAsync();

        Assert.Equal(2, all.Count());
    }
}
