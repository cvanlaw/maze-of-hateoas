using MazeOfHateoas.Domain;

namespace MazeOfHateoas.UnitTests.Domain;

public class MoveResultTests
{
    [Fact]
    public void MoveResult_HasSuccessValue()
    {
        var result = MoveResult.Success;
        Assert.Equal(MoveResult.Success, result);
    }

    [Fact]
    public void MoveResult_HasBlockedValue()
    {
        var result = MoveResult.Blocked;
        Assert.Equal(MoveResult.Blocked, result);
    }

    [Fact]
    public void MoveResult_HasOutOfBoundsValue()
    {
        var result = MoveResult.OutOfBounds;
        Assert.Equal(MoveResult.OutOfBounds, result);
    }

    [Fact]
    public void MoveResult_HasAlreadyCompletedValue()
    {
        var result = MoveResult.AlreadyCompleted;
        Assert.Equal(MoveResult.AlreadyCompleted, result);
    }

    [Fact]
    public void MoveResult_HasExactlyFourValues()
    {
        var values = Enum.GetValues<MoveResult>();
        Assert.Equal(4, values.Length);
    }
}
