using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class PositionDtoTests
{
    [Fact]
    public void Deserialize_ShouldParseJsonCorrectly()
    {
        var json = """{"x":5,"y":3}""";

        var position = JsonSerializer.Deserialize<PositionDto>(json);

        Assert.NotNull(position);
        Assert.Equal(5, position.X);
        Assert.Equal(3, position.Y);
    }
}
