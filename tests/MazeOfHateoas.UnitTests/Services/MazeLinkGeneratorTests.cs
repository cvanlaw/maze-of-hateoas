using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Api.Services;
using Xunit;

namespace MazeOfHateoas.UnitTests.Services;

public class MazeLinkGeneratorTests
{
    private readonly MazeLinkGenerator _generator = new();

    [Fact]
    public void GenerateMazeLinks_ReturnsCorrectSelfLink()
    {
        var mazeId = Guid.NewGuid();

        var links = _generator.GenerateMazeLinks(mazeId);

        Assert.True(links.ContainsKey("self"));
        var selfLink = (Link)links["self"];
        Assert.Equal($"/api/mazes/{mazeId}", selfLink.Href);
        Assert.Equal("self", selfLink.Rel);
        Assert.Equal("GET", selfLink.Method);
    }

    [Fact]
    public void GenerateMazeLinks_ReturnsCorrectStartLink()
    {
        var mazeId = Guid.NewGuid();

        var links = _generator.GenerateMazeLinks(mazeId);

        Assert.True(links.ContainsKey("start"));
        var startLink = (Link)links["start"];
        Assert.Equal($"/api/mazes/{mazeId}/sessions", startLink.Href);
        Assert.Equal("start", startLink.Rel);
        Assert.Equal("POST", startLink.Method);
    }

    [Fact]
    public void GenerateListLinks_ReturnsCorrectSelfLink()
    {
        var links = _generator.GenerateListLinks();

        Assert.True(links.ContainsKey("self"));
        var selfLink = (Link)links["self"];
        Assert.Equal("/api/mazes", selfLink.Href);
        Assert.Equal("self", selfLink.Rel);
        Assert.Equal("GET", selfLink.Method);
    }

    [Fact]
    public void GenerateListLinks_ReturnsCorrectCreateLink()
    {
        var links = _generator.GenerateListLinks();

        Assert.True(links.ContainsKey("create"));
        var createLink = (Link)links["create"];
        Assert.Equal("/api/mazes", createLink.Href);
        Assert.Equal("create", createLink.Rel);
        Assert.Equal("POST", createLink.Method);
    }
}
