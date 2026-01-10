using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MazeOfHateoas.IntegrationTests;

public class SessionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SessionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateMazeAndGetId()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 5, height = 5 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        return createJson.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task PostSession_Returns201Created()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_ReturnsSessionWithId()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var id = json.RootElement.GetProperty("id").GetString();
        Assert.NotNull(id);
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task PostSession_ReturnsSessionWithMazeId()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal(mazeId, json.RootElement.GetProperty("mazeId").GetString());
    }

    [Fact]
    public async Task PostSession_ReturnsSessionWithCurrentPositionAtStart()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var position = json.RootElement.GetProperty("currentPosition");
        Assert.Equal(0, position.GetProperty("x").GetInt32());
        Assert.Equal(0, position.GetProperty("y").GetInt32());
    }

    [Fact]
    public async Task PostSession_ReturnsSessionWithStateInProgress()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("InProgress", json.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public async Task PostSession_ReturnsSessionWithStartedAt()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var startedAt = json.RootElement.GetProperty("startedAt").GetString();
        Assert.NotNull(startedAt);
        Assert.True(DateTime.TryParse(startedAt, out _));
    }

    [Fact]
    public async Task PostSession_ReturnsLocationHeader()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var sessionId = json.RootElement.GetProperty("id").GetString();

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(mazeId, response.Headers.Location.ToString());
        Assert.Contains(sessionId!, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PostSession_WithNonExistentMaze_Returns404()
    {
        var nonExistentMazeId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/mazes/{nonExistentMazeId}/sessions", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_WithNonExistentMaze_ReturnsProblemDetails()
    {
        var nonExistentMazeId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/mazes/{nonExistentMazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task PostSession_ReturnsHateoasSelfLink()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var sessionId = json.RootElement.GetProperty("id").GetString();
        var links = json.RootElement.GetProperty("_links");
        var selfLink = links.GetProperty("self");

        Assert.Equal($"/api/mazes/{mazeId}/sessions/{sessionId}", selfLink.GetProperty("href").GetString());
        Assert.Equal("self", selfLink.GetProperty("rel").GetString());
        Assert.Equal("GET", selfLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task PostSession_DoesNotIncludeNorthLinkAtStartPosition()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        Assert.False(links.TryGetProperty("north", out _), "North link should not exist at (0,0) boundary");
    }

    [Fact]
    public async Task PostSession_DoesNotIncludeWestLinkAtStartPosition()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        Assert.False(links.TryGetProperty("west", out _), "West link should not exist at (0,0) boundary");
    }

    [Fact]
    public async Task PostSession_IncludesOnlyValidMoveLinks()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");

        // At start (0,0), we should have self link plus some move links
        // Move links should only be south/east if the cell allows (no north/west at boundary)
        Assert.True(links.TryGetProperty("self", out _), "Self link should always exist");

        // North and West should not exist at (0,0)
        Assert.False(links.TryGetProperty("north", out _));
        Assert.False(links.TryGetProperty("west", out _));

        // South and/or East may exist depending on maze generation
        // At least one should exist for a valid solvable maze
        var hasSouth = links.TryGetProperty("south", out _);
        var hasEast = links.TryGetProperty("east", out _);
        Assert.True(hasSouth || hasEast, "At least one valid move direction should exist");
    }

    [Fact]
    public async Task PostSession_MoveLinkHasCorrectFormat()
    {
        var mazeId = await CreateMazeAndGetId();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var sessionId = json.RootElement.GetProperty("id").GetString();
        var links = json.RootElement.GetProperty("_links");

        // Check if south or east link exists and has correct format
        if (links.TryGetProperty("south", out var southLink))
        {
            Assert.Equal($"/api/mazes/{mazeId}/sessions/{sessionId}/move/south", southLink.GetProperty("href").GetString());
            Assert.Equal("move", southLink.GetProperty("rel").GetString());
            Assert.Equal("POST", southLink.GetProperty("method").GetString());
        }

        if (links.TryGetProperty("east", out var eastLink))
        {
            Assert.Equal($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", eastLink.GetProperty("href").GetString());
            Assert.Equal("move", eastLink.GetProperty("rel").GetString());
            Assert.Equal("POST", eastLink.GetProperty("method").GetString());
        }
    }

    // GET Session Tests

    [Fact]
    public async Task GetSession_ReturnsOk()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_ReturnsSessionProperties()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal(sessionId, json.RootElement.GetProperty("id").GetString());
        Assert.Equal(mazeId, json.RootElement.GetProperty("mazeId").GetString());
        Assert.True(json.RootElement.TryGetProperty("currentPosition", out _));
        Assert.True(json.RootElement.TryGetProperty("state", out _));
    }

    [Fact]
    public async Task GetSession_ReturnsSelfLink()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        var selfLink = links.GetProperty("self");
        Assert.Equal($"/api/mazes/{mazeId}/sessions/{sessionId}", selfLink.GetProperty("href").GetString());
        Assert.Equal("self", selfLink.GetProperty("rel").GetString());
        Assert.Equal("GET", selfLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task GetSession_ReturnsMatchingLinksAsPost()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();
        var postLinks = postJson.RootElement.GetProperty("_links");

        var getResponse = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getJson = JsonDocument.Parse(getContent);
        var getLinks = getJson.RootElement.GetProperty("_links");

        // GET should return same links as POST for same session state
        Assert.Equal(postLinks.TryGetProperty("north", out _), getLinks.TryGetProperty("north", out _));
        Assert.Equal(postLinks.TryGetProperty("south", out _), getLinks.TryGetProperty("south", out _));
        Assert.Equal(postLinks.TryGetProperty("east", out _), getLinks.TryGetProperty("east", out _));
        Assert.Equal(postLinks.TryGetProperty("west", out _), getLinks.TryGetProperty("west", out _));
    }

    [Fact]
    public async Task GetSession_WithNonExistentSession_Returns404()
    {
        var mazeId = await CreateMazeAndGetId();
        var nonExistentSessionId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{nonExistentSessionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_WithNonExistentSession_ReturnsProblemDetails()
    {
        var mazeId = await CreateMazeAndGetId();
        var nonExistentSessionId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{nonExistentSessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(nonExistentSessionId.ToString(), json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetSession_WithNonExistentMaze_Returns404()
    {
        var nonExistentMazeId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/mazes/{nonExistentMazeId}/sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_WithNonExistentMaze_ReturnsProblemDetails()
    {
        var nonExistentMazeId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/mazes/{nonExistentMazeId}/sessions/{sessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(nonExistentMazeId.ToString(), json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetSession_DoesNotIncludeNorthLinkAtBoundary()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        Assert.False(links.TryGetProperty("north", out _), "North link should not exist at (0,0) boundary");
    }

    [Fact]
    public async Task GetSession_DoesNotIncludeWestLinkAtBoundary()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/mazes/{mazeId}/sessions/{sessionId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        Assert.False(links.TryGetProperty("west", out _), "West link should not exist at (0,0) boundary");
    }
}
