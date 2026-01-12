using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Domain;

public class MazeSessionTests
{
    [Fact]
    public void MazeSession_HasIdProperty()
    {
        var id = Guid.NewGuid();
        var mazeId = Guid.NewGuid();
        var startPosition = new Position(0, 0);

        var session = new MazeSession(id, mazeId, startPosition);

        Assert.Equal(id, session.Id);
    }

    [Fact]
    public void MazeSession_HasMazeIdProperty()
    {
        var id = Guid.NewGuid();
        var mazeId = Guid.NewGuid();
        var startPosition = new Position(0, 0);

        var session = new MazeSession(id, mazeId, startPosition);

        Assert.Equal(mazeId, session.MazeId);
    }

    [Fact]
    public void MazeSession_CurrentPosition_SetToStartPosition()
    {
        var id = Guid.NewGuid();
        var mazeId = Guid.NewGuid();
        var startPosition = new Position(3, 5);

        var session = new MazeSession(id, mazeId, startPosition);

        Assert.Equal(startPosition, session.CurrentPosition);
    }

    [Fact]
    public void MazeSession_State_SetToInProgress()
    {
        var id = Guid.NewGuid();
        var mazeId = Guid.NewGuid();
        var startPosition = new Position(0, 0);

        var session = new MazeSession(id, mazeId, startPosition);

        Assert.Equal(SessionState.InProgress, session.State);
    }

    [Fact]
    public void MazeSession_HasStartedAtProperty()
    {
        var id = Guid.NewGuid();
        var mazeId = Guid.NewGuid();
        var startPosition = new Position(0, 0);
        var beforeCreation = DateTime.UtcNow;

        var session = new MazeSession(id, mazeId, startPosition);

        Assert.True(session.StartedAt >= beforeCreation);
        Assert.True(session.StartedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void MazeSession_MoveCount_InitializedToZero()
    {
        var session = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), new Position(0, 0));

        Assert.Equal(0, session.MoveCount);
    }

    [Fact]
    public void MazeSession_VisitedCells_ContainsStartPosition()
    {
        var startPosition = new Position(0, 0);
        var session = new MazeSession(Guid.NewGuid(), Guid.NewGuid(), startPosition);

        Assert.Contains(startPosition, session.VisitedCells);
        Assert.Single(session.VisitedCells);
    }
}
