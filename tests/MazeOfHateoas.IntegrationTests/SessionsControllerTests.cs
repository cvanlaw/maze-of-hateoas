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

    // Move Endpoint Tests

    [Fact]
    public async Task Move_WithValidDirection_ReturnsOk()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();
        var links = postJson.RootElement.GetProperty("_links");

        var hasSouth = links.TryGetProperty("south", out var southLink);
        var hasEast = links.TryGetProperty("east", out var eastLink);

        string moveUrl;
        if (hasSouth)
        {
            moveUrl = southLink.GetProperty("href").GetString()!;
        }
        else if (hasEast)
        {
            moveUrl = eastLink.GetProperty("href").GetString()!;
        }
        else
        {
            Assert.Fail("No valid move direction available from starting position");
            return;
        }

        var response = await _client.PostAsync(moveUrl, null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Move_WithValidDirection_ReturnsUpdatedPosition()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();
        var links = postJson.RootElement.GetProperty("_links");

        var hasSouth = links.TryGetProperty("south", out var southLink);
        var hasEast = links.TryGetProperty("east", out var eastLink);

        string moveUrl;
        string direction;
        if (hasSouth)
        {
            moveUrl = southLink.GetProperty("href").GetString()!;
            direction = "south";
        }
        else if (hasEast)
        {
            moveUrl = eastLink.GetProperty("href").GetString()!;
            direction = "east";
        }
        else
        {
            Assert.Fail("No valid move direction available from starting position");
            return;
        }

        var response = await _client.PostAsync(moveUrl, null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var position = json.RootElement.GetProperty("currentPosition");
        if (direction == "south")
        {
            Assert.Equal(0, position.GetProperty("x").GetInt32());
            Assert.Equal(1, position.GetProperty("y").GetInt32());
        }
        else
        {
            Assert.Equal(1, position.GetProperty("x").GetInt32());
            Assert.Equal(0, position.GetProperty("y").GetInt32());
        }
    }

    [Fact]
    public async Task Move_WithValidDirection_ReturnsMoveResult()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();
        var links = postJson.RootElement.GetProperty("_links");

        var hasSouth = links.TryGetProperty("south", out var southLink);
        var hasEast = links.TryGetProperty("east", out var eastLink);

        string moveUrl = hasSouth ? southLink.GetProperty("href").GetString()! : eastLink.GetProperty("href").GetString()!;

        var response = await _client.PostAsync(moveUrl, null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Success", json.RootElement.GetProperty("moveResult").GetString());
    }

    [Fact]
    public async Task Move_AtBoundary_Returns400()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/north", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Move_AtBoundary_ReturnsProblemDetails()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/north", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Bad Request", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Move_WithInvalidDirection_Returns400()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/up", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Move_WithInvalidDirection_ReturnsProblemDetailsWithValidDirections()
    {
        var mazeId = await CreateMazeAndGetId();
        var postResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postContent);
        var sessionId = postJson.RootElement.GetProperty("id").GetString();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/up", null);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var detail = json.RootElement.GetProperty("detail").GetString();
        Assert.Contains("north", detail);
        Assert.Contains("south", detail);
        Assert.Contains("east", detail);
        Assert.Contains("west", detail);
    }

    [Fact]
    public async Task Move_WithNonExistentSession_Returns404()
    {
        var mazeId = await CreateMazeAndGetId();
        var nonExistentSessionId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{nonExistentSessionId}/move/north", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Move_WithNonExistentMaze_Returns404()
    {
        var nonExistentMazeId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/mazes/{nonExistentMazeId}/sessions/{sessionId}/move/north", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Move_NavigateToEnd_SetsStateToCompleted()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 2, height = 1 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var mazeId = createJson.RootElement.GetProperty("id").GetString();

        var sessionResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
        var sessionJson = JsonDocument.Parse(sessionContent);
        var sessionId = sessionJson.RootElement.GetProperty("id").GetString();

        var moveResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", null);
        var moveContent = await moveResponse.Content.ReadAsStringAsync();
        var moveJson = JsonDocument.Parse(moveContent);

        Assert.Equal("Completed", moveJson.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public async Task Move_NavigateToEnd_ReturnsCompletionMessage()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 2, height = 1 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var mazeId = createJson.RootElement.GetProperty("id").GetString();

        var sessionResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
        var sessionJson = JsonDocument.Parse(sessionContent);
        var sessionId = sessionJson.RootElement.GetProperty("id").GetString();

        var moveResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", null);
        var moveContent = await moveResponse.Content.ReadAsStringAsync();
        var moveJson = JsonDocument.Parse(moveContent);

        Assert.True(moveJson.RootElement.TryGetProperty("message", out var message));
        Assert.Contains("Congratulations", message.GetString());
    }

    [Fact]
    public async Task Move_NavigateToEnd_ReturnsCompletionLinks()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 2, height = 1 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var mazeId = createJson.RootElement.GetProperty("id").GetString();

        var sessionResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
        var sessionJson = JsonDocument.Parse(sessionContent);
        var sessionId = sessionJson.RootElement.GetProperty("id").GetString();

        var moveResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", null);
        var moveContent = await moveResponse.Content.ReadAsStringAsync();
        var moveJson = JsonDocument.Parse(moveContent);

        var links = moveJson.RootElement.GetProperty("_links");
        Assert.True(links.TryGetProperty("mazes", out _));
        Assert.True(links.TryGetProperty("newMaze", out _));
        Assert.False(links.TryGetProperty("north", out _));
        Assert.False(links.TryGetProperty("south", out _));
        Assert.False(links.TryGetProperty("east", out _));
        Assert.False(links.TryGetProperty("west", out _));
    }

    [Fact]
    public async Task Move_OnCompletedSession_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 2, height = 1 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var mazeId = createJson.RootElement.GetProperty("id").GetString();

        var sessionResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
        var sessionJson = JsonDocument.Parse(sessionContent);
        var sessionId = sessionJson.RootElement.GetProperty("id").GetString();

        await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", null);

        var secondMoveResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/west", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondMoveResponse.StatusCode);
    }

    [Fact]
    public async Task Move_OnCompletedSession_ReturnsProblemDetailsWithAlreadyCompleted()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", new { width = 2, height = 1 });
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var mazeId = createJson.RootElement.GetProperty("id").GetString();

        var sessionResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions", null);
        var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
        var sessionJson = JsonDocument.Parse(sessionContent);
        var sessionId = sessionJson.RootElement.GetProperty("id").GetString();

        await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/east", null);

        var secondMoveResponse = await _client.PostAsync($"/api/mazes/{mazeId}/sessions/{sessionId}/move/west", null);
        var content = await secondMoveResponse.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal("Bad Request", json.RootElement.GetProperty("title").GetString());
        Assert.Contains("already completed", json.RootElement.GetProperty("detail").GetString());
    }
}
