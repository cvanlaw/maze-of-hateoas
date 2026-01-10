using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Domain;

public class SessionStateTests
{
    [Fact]
    public void SessionState_HasInProgressValue()
    {
        var state = SessionState.InProgress;
        Assert.Equal(SessionState.InProgress, state);
    }

    [Fact]
    public void SessionState_HasCompletedValue()
    {
        var state = SessionState.Completed;
        Assert.Equal(SessionState.Completed, state);
    }

    [Fact]
    public void SessionState_HasExactlyTwoValues()
    {
        var values = Enum.GetValues<SessionState>();
        Assert.Equal(2, values.Length);
    }
}
