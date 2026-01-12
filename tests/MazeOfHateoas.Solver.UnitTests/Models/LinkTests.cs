using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class LinkTests
{
    [Fact]
    public void Deserialize_ShouldParseJsonCorrectly()
    {
        var json = """{"href":"/api/mazes","rel":"self","method":"GET"}""";

        var link = JsonSerializer.Deserialize<Link>(json);

        Assert.NotNull(link);
        Assert.Equal("/api/mazes", link.Href);
        Assert.Equal("self", link.Rel);
        Assert.Equal("GET", link.Method);
    }
}
