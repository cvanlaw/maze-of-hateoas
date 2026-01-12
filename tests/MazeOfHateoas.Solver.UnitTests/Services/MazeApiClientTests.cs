using System.Net;
using System.Text.Json;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class MazeApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<MazeApiClient>> _mockLogger;

    public MazeApiClientTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        _mockLogger = new Mock<ILogger<MazeApiClient>>();
    }

    [Fact]
    public async Task CreateMazeAsync_ShouldPostAndReturnMaze()
    {
        var expectedMaze = new MazeResponse
        {
            Id = Guid.NewGuid(),
            Width = 10,
            Height = 10,
            Start = new PositionDto { X = 0, Y = 0 },
            End = new PositionDto { X = 9, Y = 9 },
            CreatedAt = DateTime.UtcNow,
            Links = new Dictionary<string, Link>
            {
                ["start"] = new Link { Href = "/api/mazes/123/sessions", Rel = "start", Method = "POST" }
            }
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/mazes"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(expectedMaze))
            });

        var client = new MazeApiClient(_httpClient, _mockLogger.Object);

        var result = await client.CreateMazeAsync(10, 10);

        Assert.Equal(expectedMaze.Id, result.Id);
        Assert.Equal(10, result.Width);
        Assert.True(result.Links.ContainsKey("start"));
    }

    [Fact]
    public async Task StartSessionAsync_ShouldUseExactLinkHref()
    {
        var link = new Link { Href = "/api/mazes/abc/sessions", Rel = "start", Method = "POST" };
        var expectedSession = new SessionResponse
        {
            Id = Guid.NewGuid(),
            MazeId = Guid.NewGuid(),
            CurrentPosition = new PositionDto { X = 0, Y = 0 },
            State = "InProgress",
            Links = new Dictionary<string, Link>()
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/mazes/abc/sessions"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(expectedSession))
            });

        var client = new MazeApiClient(_httpClient, _mockLogger.Object);

        var result = await client.StartSessionAsync(link);

        Assert.Equal(expectedSession.Id, result.Id);
    }

    [Fact]
    public async Task MoveAsync_ShouldUseExactLinkHref()
    {
        var link = new Link { Href = "/api/mazes/abc/sessions/xyz/move/north", Rel = "move", Method = "POST" };
        var expectedSession = new SessionResponse
        {
            Id = Guid.NewGuid(),
            MazeId = Guid.NewGuid(),
            CurrentPosition = new PositionDto { X = 0, Y = 1 },
            State = "InProgress",
            MoveResult = "Success",
            Links = new Dictionary<string, Link>()
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/mazes/abc/sessions/xyz/move/north"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedSession))
            });

        var client = new MazeApiClient(_httpClient, _mockLogger.Object);

        var result = await client.MoveAsync(link);

        Assert.Equal("Success", result.MoveResult);
    }
}
