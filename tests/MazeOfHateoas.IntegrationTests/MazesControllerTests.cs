using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MazeOfHateoas.IntegrationTests;

public class MazesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MazesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostMaze_Returns201Created()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostMaze_ReturnsMazeWithCorrectDimensions()
    {
        var request = new { width = 7, height = 9 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal(7, json.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(9, json.RootElement.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task PostMaze_ReturnsMazeWithId()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var id = json.RootElement.GetProperty("id").GetString();
        Assert.NotNull(id);
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task PostMaze_ReturnsMazeWithStartPosition()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var start = json.RootElement.GetProperty("start");
        Assert.Equal(0, start.GetProperty("x").GetInt32());
        Assert.Equal(0, start.GetProperty("y").GetInt32());
    }

    [Fact]
    public async Task PostMaze_ReturnsMazeWithEndPosition()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var end = json.RootElement.GetProperty("end");
        Assert.Equal(4, end.GetProperty("x").GetInt32());
        Assert.Equal(4, end.GetProperty("y").GetInt32());
    }

    [Fact]
    public async Task PostMaze_ReturnsMazeWithCreatedAt()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var createdAt = json.RootElement.GetProperty("createdAt").GetString();
        Assert.NotNull(createdAt);
        Assert.True(DateTime.TryParse(createdAt, out _));
    }

    [Fact]
    public async Task PostMaze_ReturnsHateoasSelfLink()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var id = json.RootElement.GetProperty("id").GetString();
        var links = json.RootElement.GetProperty("_links");
        var selfLink = links.GetProperty("self");

        Assert.Equal($"/api/mazes/{id}", selfLink.GetProperty("href").GetString());
        Assert.Equal("self", selfLink.GetProperty("rel").GetString());
        Assert.Equal("GET", selfLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task PostMaze_ReturnsHateoasStartLink()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var id = json.RootElement.GetProperty("id").GetString();
        var links = json.RootElement.GetProperty("_links");
        var startLink = links.GetProperty("start");

        Assert.Equal($"/api/mazes/{id}/sessions", startLink.GetProperty("href").GetString());
        Assert.Equal("start", startLink.GetProperty("rel").GetString());
        Assert.Equal("POST", startLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task PostMaze_WithNoBody_UsesDefaultDimensions()
    {
        var response = await _client.PostAsJsonAsync("/api/mazes", new { });
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(10, json.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(10, json.RootElement.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task PostMaze_LocationHeaderPointsToCreatedMaze()
    {
        var request = new { width = 5, height = 5 };

        var response = await _client.PostAsJsonAsync("/api/mazes", request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetString();

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(id!, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task GetMaze_ReturnsCreatedMaze()
    {
        var createRequest = new { width = 5, height = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var id = createJson.RootElement.GetProperty("id").GetString();

        var getResponse = await _client.GetAsync($"/api/mazes/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getJson = JsonDocument.Parse(getContent);
        Assert.Equal(id, getJson.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetMaze_WithInvalidId_Returns404()
    {
        var response = await _client.GetAsync($"/api/mazes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaze_WithInvalidId_ReturnsProblemDetails()
    {
        var invalidId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/mazes/{invalidId}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(invalidId.ToString(), json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetMazes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/mazes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMazes_ReturnsCollectionWithMazesArray()
    {
        var response = await _client.GetAsync("/api/mazes");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("mazes", out var mazesProperty));
        Assert.Equal(JsonValueKind.Array, mazesProperty.ValueKind);
    }

    [Fact]
    public async Task GetMazes_ReturnsCollectionWithSelfLink()
    {
        var response = await _client.GetAsync("/api/mazes");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        var selfLink = links.GetProperty("self");

        Assert.Equal("/api/mazes", selfLink.GetProperty("href").GetString());
        Assert.Equal("self", selfLink.GetProperty("rel").GetString());
        Assert.Equal("GET", selfLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task GetMazes_ReturnsCollectionWithCreateLink()
    {
        var response = await _client.GetAsync("/api/mazes");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var links = json.RootElement.GetProperty("_links");
        var createLink = links.GetProperty("create");

        Assert.Equal("/api/mazes", createLink.GetProperty("href").GetString());
        Assert.Equal("create", createLink.GetProperty("rel").GetString());
        Assert.Equal("POST", createLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task GetMazes_AfterCreatingMaze_ReturnsMazeInList()
    {
        var createRequest = new { width = 5, height = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var createdId = createJson.RootElement.GetProperty("id").GetString();

        var listResponse = await _client.GetAsync("/api/mazes");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        var mazes = listJson.RootElement.GetProperty("mazes");
        var found = false;
        foreach (var maze in mazes.EnumerateArray())
        {
            if (maze.GetProperty("id").GetString() == createdId)
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "Created maze should appear in list");
    }

    [Fact]
    public async Task GetMazes_MazeSummaryIncludesRequiredProperties()
    {
        var createRequest = new { width = 5, height = 5 };
        await _client.PostAsJsonAsync("/api/mazes", createRequest);

        var listResponse = await _client.GetAsync("/api/mazes");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        var mazes = listJson.RootElement.GetProperty("mazes");
        Assert.True(mazes.GetArrayLength() > 0);

        var firstMaze = mazes[0];
        Assert.True(firstMaze.TryGetProperty("id", out _));
        Assert.True(firstMaze.TryGetProperty("width", out _));
        Assert.True(firstMaze.TryGetProperty("height", out _));
        Assert.True(firstMaze.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task GetMazes_MazeSummaryIncludesSelfLink()
    {
        var createRequest = new { width = 5, height = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var createdId = createJson.RootElement.GetProperty("id").GetString();

        var listResponse = await _client.GetAsync("/api/mazes");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        var mazes = listJson.RootElement.GetProperty("mazes");
        JsonElement? targetMaze = null;
        foreach (var maze in mazes.EnumerateArray())
        {
            if (maze.GetProperty("id").GetString() == createdId)
            {
                targetMaze = maze;
                break;
            }
        }

        Assert.NotNull(targetMaze);
        var links = targetMaze.Value.GetProperty("_links");
        var selfLink = links.GetProperty("self");
        Assert.Equal($"/api/mazes/{createdId}", selfLink.GetProperty("href").GetString());
        Assert.Equal("self", selfLink.GetProperty("rel").GetString());
        Assert.Equal("GET", selfLink.GetProperty("method").GetString());
    }

    [Fact]
    public async Task GetMazes_MazeSummaryIncludesStartLink()
    {
        var createRequest = new { width = 5, height = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/mazes", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var createdId = createJson.RootElement.GetProperty("id").GetString();

        var listResponse = await _client.GetAsync("/api/mazes");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        var mazes = listJson.RootElement.GetProperty("mazes");
        JsonElement? targetMaze = null;
        foreach (var maze in mazes.EnumerateArray())
        {
            if (maze.GetProperty("id").GetString() == createdId)
            {
                targetMaze = maze;
                break;
            }
        }

        Assert.NotNull(targetMaze);
        var links = targetMaze.Value.GetProperty("_links");
        var startLink = links.GetProperty("start");
        Assert.Equal($"/api/mazes/{createdId}/sessions", startLink.GetProperty("href").GetString());
        Assert.Equal("start", startLink.GetProperty("rel").GetString());
        Assert.Equal("POST", startLink.GetProperty("method").GetString());
    }
}
