using System.Text.Json;
using Xunit;

namespace MazeOfHateoas.IntegrationTests.Helpers;

public static class TestHelpers
{
    public static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    public static void AssertLink(JsonElement links, string key,
        string expectedHref, string expectedRel, string expectedMethod)
    {
        Assert.True(links.TryGetProperty(key, out var link), $"Link '{key}' not found");
        Assert.Equal(expectedHref, link.GetProperty("href").GetString());
        Assert.Equal(expectedRel, link.GetProperty("rel").GetString());
        Assert.Equal(expectedMethod, link.GetProperty("method").GetString());
    }

    public static void AssertProblemDetails(JsonElement root,
        string expectedTitle, int expectedStatus)
    {
        Assert.Equal(expectedTitle, root.GetProperty("title").GetString());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
    }

    public static string GetId(JsonDocument json) =>
        json.RootElement.GetProperty("id").GetString()!;

    public static JsonElement GetLinks(JsonDocument json) =>
        json.RootElement.GetProperty("_links");
}
