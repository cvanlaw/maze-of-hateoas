using MazeOfHateoas.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MazeOfHateoas.UnitTests.Helpers;

public class ProblemDetailsFactoryTests
{
    [Fact]
    public void BadRequest_ReturnsCorrectProblemDetails()
    {
        var result = ProblemDetailsFactory.BadRequest("Test detail", "/api/test");

        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", result.Type);
        Assert.Equal("Bad Request", result.Title);
        Assert.Equal(400, result.Status);
        Assert.Equal("Test detail", result.Detail);
        Assert.Equal("/api/test", result.Instance);
    }

    [Fact]
    public void NotFound_ReturnsCorrectProblemDetails()
    {
        var result = ProblemDetailsFactory.NotFound("Not found detail", "/api/items/123");

        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", result.Type);
        Assert.Equal("Not Found", result.Title);
        Assert.Equal(404, result.Status);
        Assert.Equal("Not found detail", result.Detail);
        Assert.Equal("/api/items/123", result.Instance);
    }
}
