# Maze Solver Console Application - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a console application that continuously generates and solves mazes via the HATEOAS API, demonstrating proper hypermedia client behavior.

**Architecture:** Hosted service pattern with `IHostedService` for graceful shutdown. Pure HTTP client that navigates exclusively through HATEOAS links. DFS algorithm with backtracking tracks visited cells to efficiently solve mazes.

**Tech Stack:** .NET 8, Microsoft.Extensions.Hosting, Microsoft.Extensions.Http, Serilog, Polly (via Microsoft.Extensions.Http.Resilience)

**Design Doc:** `docs/plans/2026-01-12-maze-solver-design.md`

---

## Task 1: Create Solver Project

**Files:**
- Create: `src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj`
- Modify: `MazeOfHateoas.sln`

**Step 1: Create project directory**

```bash
mkdir -p src/MazeOfHateoas.Solver
```

**Step 2: Create project file**

Create `src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  </ItemGroup>

</Project>
```

**Step 3: Add project to solution**

Run: `dotnet sln MazeOfHateoas.sln add src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj`

**Step 4: Create placeholder Program.cs**

Create `src/MazeOfHateoas.Solver/Program.cs`:

```csharp
Console.WriteLine("Maze Solver - placeholder");
```

**Step 5: Verify project builds in container**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet build src/MazeOfHateoas.Solver/`

Expected: Build succeeded

**Step 6: Commit**

```bash
git add src/MazeOfHateoas.Solver/ MazeOfHateoas.sln
git commit -m "feat(solver): create MazeOfHateoas.Solver project"
```

---

## Task 2: Create Solver Unit Tests Project

**Files:**
- Create: `tests/MazeOfHateoas.Solver.UnitTests/MazeOfHateoas.Solver.UnitTests.csproj`
- Modify: `MazeOfHateoas.sln`

**Step 1: Create test project directory**

```bash
mkdir -p tests/MazeOfHateoas.Solver.UnitTests
```

**Step 2: Create test project file**

Create `tests/MazeOfHateoas.Solver.UnitTests/MazeOfHateoas.Solver.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MazeOfHateoas.Solver\MazeOfHateoas.Solver.csproj" />
  </ItemGroup>

</Project>
```

**Step 3: Add test project to solution**

Run: `dotnet sln MazeOfHateoas.sln add tests/MazeOfHateoas.Solver.UnitTests/MazeOfHateoas.Solver.UnitTests.csproj`

**Step 4: Create placeholder test**

Create `tests/MazeOfHateoas.Solver.UnitTests/PlaceholderTests.cs`:

```csharp
namespace MazeOfHateoas.Solver.UnitTests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        Assert.True(true);
    }
}
```

**Step 5: Run tests in container**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 6: Commit**

```bash
git add tests/MazeOfHateoas.Solver.UnitTests/ MazeOfHateoas.sln
git commit -m "feat(solver): create MazeOfHateoas.Solver.UnitTests project"
```

---

## Task 3: SolverSettings Configuration

**Files:**
- Create: `src/MazeOfHateoas.Solver/Configuration/SolverSettings.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Configuration/SolverSettingsTests.cs`

**Step 1: Write failing test for SolverSettings defaults**

Create `tests/MazeOfHateoas.Solver.UnitTests/Configuration/SolverSettingsTests.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;

namespace MazeOfHateoas.Solver.UnitTests.Configuration;

public class SolverSettingsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var settings = new SolverSettings();

        Assert.Equal("http://localhost:8080", settings.ApiBaseUrl);
        Assert.Equal(10, settings.MazeWidth);
        Assert.Equal(10, settings.MazeHeight);
        Assert.Equal(2000, settings.DelayBetweenMazesMs);
        Assert.Equal(0, settings.DelayBetweenMovesMs);
        Assert.Equal(10, settings.StatsIntervalMazes);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverSettingsTests" --verbosity normal`

Expected: FAIL - type or namespace 'Configuration' does not exist

**Step 3: Create SolverSettings**

Create `src/MazeOfHateoas.Solver/Configuration/SolverSettings.cs`:

```csharp
namespace MazeOfHateoas.Solver.Configuration;

public class SolverSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:8080";
    public int MazeWidth { get; set; } = 10;
    public int MazeHeight { get; set; } = 10;
    public int DelayBetweenMazesMs { get; set; } = 2000;
    public int DelayBetweenMovesMs { get; set; } = 0;
    public int StatsIntervalMazes { get; set; } = 10;
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverSettingsTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Configuration/ tests/MazeOfHateoas.Solver.UnitTests/Configuration/
git commit -m "feat(solver): add SolverSettings configuration class"
```

---

## Task 4: Link Model

**Files:**
- Create: `src/MazeOfHateoas.Solver/Models/Link.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Models/LinkTests.cs`

**Step 1: Write failing test for Link deserialization**

Create `tests/MazeOfHateoas.Solver.UnitTests/Models/LinkTests.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "LinkTests" --verbosity normal`

Expected: FAIL - type or namespace 'Models' does not exist

**Step 3: Create Link model**

Create `src/MazeOfHateoas.Solver/Models/Link.cs`:

```csharp
using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record Link
{
    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("rel")]
    public required string Rel { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "LinkTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Models/ tests/MazeOfHateoas.Solver.UnitTests/Models/
git commit -m "feat(solver): add Link model for HATEOAS navigation"
```

---

## Task 5: PositionDto Model

**Files:**
- Create: `src/MazeOfHateoas.Solver/Models/PositionDto.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Models/PositionDtoTests.cs`

**Step 1: Write failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Models/PositionDtoTests.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "PositionDtoTests" --verbosity normal`

Expected: FAIL - type 'PositionDto' could not be found

**Step 3: Create PositionDto**

Create `src/MazeOfHateoas.Solver/Models/PositionDto.cs`:

```csharp
using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record PositionDto
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "PositionDtoTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Models/PositionDto.cs tests/MazeOfHateoas.Solver.UnitTests/Models/PositionDtoTests.cs
git commit -m "feat(solver): add PositionDto model"
```

---

## Task 6: MazeResponse Model

**Files:**
- Create: `src/MazeOfHateoas.Solver/Models/MazeResponse.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Models/MazeResponseTests.cs`

**Step 1: Write failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Models/MazeResponseTests.cs`:

```csharp
using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class MazeResponseTests
{
    [Fact]
    public void Deserialize_ShouldParseJsonWithLinks()
    {
        var json = """
        {
            "id": "550e8400-e29b-41d4-a716-446655440000",
            "width": 10,
            "height": 10,
            "start": {"x": 0, "y": 0},
            "end": {"x": 9, "y": 9},
            "createdAt": "2026-01-12T10:30:00Z",
            "_links": {
                "self": {"href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET"},
                "start": {"href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST"}
            }
        }
        """;

        var maze = JsonSerializer.Deserialize<MazeResponse>(json);

        Assert.NotNull(maze);
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), maze.Id);
        Assert.Equal(10, maze.Width);
        Assert.Equal(10, maze.Height);
        Assert.Equal(0, maze.Start.X);
        Assert.Equal(9, maze.End.Y);
        Assert.True(maze.Links.ContainsKey("start"));
        Assert.Equal("/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", maze.Links["start"].Href);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "MazeResponseTests" --verbosity normal`

Expected: FAIL - type 'MazeResponse' could not be found

**Step 3: Create MazeResponse**

Create `src/MazeOfHateoas.Solver/Models/MazeResponse.cs`:

```csharp
using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record MazeResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("start")]
    public required PositionDto Start { get; init; }

    [JsonPropertyName("end")]
    public required PositionDto End { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; init; } = new();
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "MazeResponseTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Models/MazeResponse.cs tests/MazeOfHateoas.Solver.UnitTests/Models/MazeResponseTests.cs
git commit -m "feat(solver): add MazeResponse model"
```

---

## Task 7: SessionResponse Model

**Files:**
- Create: `src/MazeOfHateoas.Solver/Models/SessionResponse.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Models/SessionResponseTests.cs`

**Step 1: Write failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Models/SessionResponseTests.cs`:

```csharp
using System.Text.Json;
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.UnitTests.Models;

public class SessionResponseTests
{
    [Fact]
    public void Deserialize_ShouldParseInProgressSession()
    {
        var json = """
        {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "mazeId": "550e8400-e29b-41d4-a716-446655440000",
            "currentPosition": {"x": 2, "y": 3},
            "state": "InProgress",
            "_links": {
                "self": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4", "rel": "self", "method": "GET"},
                "north": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4/move/north", "rel": "move", "method": "POST"},
                "east": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4/move/east", "rel": "move", "method": "POST"}
            }
        }
        """;

        var session = JsonSerializer.Deserialize<SessionResponse>(json);

        Assert.NotNull(session);
        Assert.Equal(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), session.Id);
        Assert.Equal("InProgress", session.State);
        Assert.Equal(2, session.CurrentPosition.X);
        Assert.Equal(3, session.CurrentPosition.Y);
        Assert.True(session.Links.ContainsKey("north"));
        Assert.True(session.Links.ContainsKey("east"));
        Assert.False(session.Links.ContainsKey("south"));
        Assert.False(session.Links.ContainsKey("west"));
    }

    [Fact]
    public void Deserialize_ShouldParseCompletedSession()
    {
        var json = """
        {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "mazeId": "550e8400-e29b-41d4-a716-446655440000",
            "currentPosition": {"x": 9, "y": 9},
            "state": "Completed",
            "message": "Congratulations! You've completed the maze!",
            "_links": {
                "self": {"href": "/api/mazes/550e8400/sessions/a1b2c3d4", "rel": "self", "method": "GET"}
            }
        }
        """;

        var session = JsonSerializer.Deserialize<SessionResponse>(json);

        Assert.NotNull(session);
        Assert.Equal("Completed", session.State);
        Assert.Equal("Congratulations! You've completed the maze!", session.Message);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SessionResponseTests" --verbosity normal`

Expected: FAIL - type 'SessionResponse' could not be found

**Step 3: Create SessionResponse**

Create `src/MazeOfHateoas.Solver/Models/SessionResponse.cs`:

```csharp
using System.Text.Json.Serialization;

namespace MazeOfHateoas.Solver.Models;

public record SessionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("mazeId")]
    public Guid MazeId { get; init; }

    [JsonPropertyName("currentPosition")]
    public required PositionDto CurrentPosition { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("moveResult")]
    public string? MoveResult { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link> Links { get; init; } = new();
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SessionResponseTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 2

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Models/SessionResponse.cs tests/MazeOfHateoas.Solver.UnitTests/Models/SessionResponseTests.cs
git commit -m "feat(solver): add SessionResponse model"
```

---

## Task 8: IMazeApiClient Interface

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/IMazeApiClient.cs`

**Step 1: Create interface**

Create `src/MazeOfHateoas.Solver/Services/IMazeApiClient.cs`:

```csharp
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.Services;

public interface IMazeApiClient
{
    Task<MazeResponse> CreateMazeAsync(int width, int height, CancellationToken ct = default);
    Task<SessionResponse> StartSessionAsync(Link startLink, CancellationToken ct = default);
    Task<SessionResponse> MoveAsync(Link moveLink, CancellationToken ct = default);
}
```

**Step 2: Verify build**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet build src/MazeOfHateoas.Solver/`

Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/
git commit -m "feat(solver): add IMazeApiClient interface"
```

---

## Task 9: MazeApiClient Implementation

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/MazeApiClient.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/MazeApiClientTests.cs`

**Step 1: Write failing test for CreateMazeAsync**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/MazeApiClientTests.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "MazeApiClientTests" --verbosity normal`

Expected: FAIL - type 'MazeApiClient' could not be found

**Step 3: Create MazeApiClient**

Create `src/MazeOfHateoas.Solver/Services/MazeApiClient.cs`:

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;

namespace MazeOfHateoas.Solver.Services;

public class MazeApiClient : IMazeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MazeApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MazeApiClient(HttpClient httpClient, ILogger<MazeApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MazeResponse> CreateMazeAsync(int width, int height, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating maze {Width}x{Height}", width, height);

        var response = await _httpClient.PostAsJsonAsync("/api/mazes", new { width, height }, ct);
        response.EnsureSuccessStatusCode();

        var maze = await response.Content.ReadFromJsonAsync<MazeResponse>(JsonOptions, ct);
        return maze ?? throw new InvalidOperationException("Failed to deserialize maze response");
    }

    public async Task<SessionResponse> StartSessionAsync(Link startLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Starting session via {Href}", startLink.Href);

        var response = await _httpClient.PostAsync(startLink.Href, null, ct);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, ct);
        return session ?? throw new InvalidOperationException("Failed to deserialize session response");
    }

    public async Task<SessionResponse> MoveAsync(Link moveLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Moving via {Href}", moveLink.Href);

        var response = await _httpClient.PostAsync(moveLink.Href, null, ct);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, ct);
        return session ?? throw new InvalidOperationException("Failed to deserialize session response");
    }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "MazeApiClientTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 3

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/MazeApiClient.cs tests/MazeOfHateoas.Solver.UnitTests/Services/
git commit -m "feat(solver): add MazeApiClient implementation"
```

---

## Task 10: ISolver Interface

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/ISolver.cs`
- Create: `src/MazeOfHateoas.Solver/Services/SolveResult.cs`

**Step 1: Create SolveResult**

Create `src/MazeOfHateoas.Solver/Services/SolveResult.cs`:

```csharp
namespace MazeOfHateoas.Solver.Services;

public record SolveResult(
    Guid MazeId,
    Guid SessionId,
    int MoveCount,
    long ElapsedMs,
    bool Success
);
```

**Step 2: Create ISolver interface**

Create `src/MazeOfHateoas.Solver/Services/ISolver.cs`:

```csharp
using MazeOfHateoas.Solver.Models;

namespace MazeOfHateoas.Solver.Services;

public interface ISolver
{
    Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default);
}
```

**Step 3: Verify build**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet build src/MazeOfHateoas.Solver/`

Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/ISolver.cs src/MazeOfHateoas.Solver/Services/SolveResult.cs
git commit -m "feat(solver): add ISolver interface and SolveResult"
```

---

## Task 11: HateoasSolver - Basic Navigation

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/HateoasSolver.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs`

**Step 1: Write failing test for simple maze solve**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class HateoasSolverTests
{
    private readonly Mock<IMazeApiClient> _mockApiClient;
    private readonly Mock<ILogger<HateoasSolver>> _mockLogger;
    private readonly IOptions<SolverSettings> _settings;
    private readonly Guid _mazeId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public HateoasSolverTests()
    {
        _mockApiClient = new Mock<IMazeApiClient>();
        _mockLogger = new Mock<ILogger<HateoasSolver>>();
        _settings = Options.Create(new SolverSettings { DelayBetweenMovesMs = 0 });
    }

    [Fact]
    public async Task SolveAsync_WhenAlreadyAtEnd_ReturnsImmediately()
    {
        var maze = CreateMaze();
        var completedSession = CreateSession(9, 9, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(0, result.MoveCount);
    }

    [Fact]
    public async Task SolveAsync_WhenOneMove_CompletesInOneMove()
    {
        var maze = CreateMaze();
        var startSession = CreateSession(0, 0, "InProgress", ("east", "/move/east"));
        var endSession = CreateSession(1, 0, "Completed");

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(startSession);
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(endSession);

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(1, result.MoveCount);
    }

    private MazeResponse CreateMaze() => new()
    {
        Id = _mazeId,
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

    private SessionResponse CreateSession(int x, int y, string state, params (string name, string href)[] moves) => new()
    {
        Id = _sessionId,
        MazeId = _mazeId,
        CurrentPosition = new PositionDto { X = x, Y = y },
        State = state,
        Links = moves.ToDictionary(
            m => m.name,
            m => new Link { Href = m.href, Rel = "move", Method = "POST" }
        )
    };
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "HateoasSolverTests" --verbosity normal`

Expected: FAIL - type 'HateoasSolver' could not be found

**Step 3: Create HateoasSolver with basic navigation**

Create `src/MazeOfHateoas.Solver/Services/HateoasSolver.cs`:

```csharp
using System.Diagnostics;
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class HateoasSolver : ISolver
{
    private static readonly string[] Directions = ["north", "south", "east", "west"];

    private readonly IMazeApiClient _apiClient;
    private readonly SolverSettings _settings;
    private readonly ILogger<HateoasSolver> _logger;

    public HateoasSolver(
        IMazeApiClient apiClient,
        IOptions<SolverSettings> settings,
        ILogger<HateoasSolver> logger)
    {
        _apiClient = apiClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SolveResult> SolveAsync(MazeResponse maze, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var moveCount = 0;

        var startLink = maze.Links["start"];
        var session = await _apiClient.StartSessionAsync(startLink, ct);

        _logger.LogInformation("Started maze {MazeId}, session {SessionId} at ({X},{Y})",
            maze.Id, session.Id, session.CurrentPosition.X, session.CurrentPosition.Y);

        var visited = new HashSet<(int X, int Y)>();
        var backtrackStack = new Stack<(int X, int Y)>();
        visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));

        while (session.State != "Completed" && !ct.IsCancellationRequested)
        {
            var currentPos = (session.CurrentPosition.X, session.CurrentPosition.Y);
            var availableMoves = GetAvailableMoves(session);
            var unvisitedMoves = availableMoves
                .Where(m => !visited.Contains(GetTargetPosition(currentPos, m.direction)))
                .ToList();

            Link? moveLink;
            string direction;

            if (unvisitedMoves.Count > 0)
            {
                backtrackStack.Push(currentPos);
                (direction, moveLink) = unvisitedMoves[0];
            }
            else if (backtrackStack.Count > 0)
            {
                var backtrackTo = backtrackStack.Pop();
                (direction, moveLink) = GetMoveToward(currentPos, backtrackTo, availableMoves);
            }
            else
            {
                _logger.LogWarning("No moves available and backtrack stack empty at ({X},{Y})",
                    currentPos.X, currentPos.Y);
                break;
            }

            var targetPos = GetTargetPosition(currentPos, direction);
            _logger.LogDebug("Moving {Direction} from ({FromX},{FromY}) to ({ToX},{ToY}), visited: {VisitedCount}",
                direction, currentPos.X, currentPos.Y, targetPos.X, targetPos.Y, visited.Count);

            session = await _apiClient.MoveAsync(moveLink, ct);
            visited.Add((session.CurrentPosition.X, session.CurrentPosition.Y));
            moveCount++;

            if (_settings.DelayBetweenMovesMs > 0)
                await Task.Delay(_settings.DelayBetweenMovesMs, ct);
        }

        stopwatch.Stop();
        var success = session.State == "Completed";

        _logger.LogInformation("Maze {MazeId} {Result} in {MoveCount} moves ({ElapsedMs}ms)",
            maze.Id, success ? "solved" : "failed", moveCount, stopwatch.ElapsedMilliseconds);

        return new SolveResult(maze.Id, session.Id, moveCount, stopwatch.ElapsedMilliseconds, success);
    }

    private static List<(string direction, Link link)> GetAvailableMoves(SessionResponse session) =>
        Directions
            .Where(d => session.Links.ContainsKey(d))
            .Select(d => (d, session.Links[d]))
            .ToList();

    private static (int X, int Y) GetTargetPosition((int X, int Y) from, string direction) => direction switch
    {
        "north" => (from.X, from.Y - 1),
        "south" => (from.X, from.Y + 1),
        "east" => (from.X + 1, from.Y),
        "west" => (from.X - 1, from.Y),
        _ => from
    };

    private static (string direction, Link link) GetMoveToward(
        (int X, int Y) from,
        (int X, int Y) target,
        List<(string direction, Link link)> availableMoves)
    {
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;

        string preferredDirection;
        if (dx > 0) preferredDirection = "east";
        else if (dx < 0) preferredDirection = "west";
        else if (dy > 0) preferredDirection = "south";
        else preferredDirection = "north";

        return availableMoves.FirstOrDefault(m => m.direction == preferredDirection);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "HateoasSolverTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 2

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/HateoasSolver.cs tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs
git commit -m "feat(solver): add HateoasSolver with DFS backtracking"
```

---

## Task 12: HateoasSolver - Backtracking Tests

**Files:**
- Modify: `tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs`

**Step 1: Add test for backtracking behavior**

Add to `tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs`:

```csharp
    [Fact]
    public async Task SolveAsync_WhenDeadEnd_BacktracksToLastJunction()
    {
        // Maze: Start(0,0) -> (1,0) dead end, backtrack, go (0,1) -> End
        var maze = CreateMaze();

        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // start
            CreateSession(1, 0, "InProgress", ("west", "/move/west")), // dead end - only way back
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // back at start
            CreateSession(0, 1, "Completed") // found exit going south
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        var result = await solver.SolveAsync(maze);

        Assert.True(result.Success);
        Assert.Equal(3, result.MoveCount); // east, west (backtrack), south
    }

    [Fact]
    public async Task SolveAsync_ChoosesUnvisitedOverVisited()
    {
        var maze = CreateMaze();

        var linkCaptures = new List<string>();
        var moveSequence = new Queue<SessionResponse>(new[]
        {
            CreateSession(0, 0, "InProgress", ("east", "/move/east"), ("south", "/move/south")), // start
            CreateSession(1, 0, "Completed") // end after going east
        });

        _mockApiClient.Setup(c => c.StartSessionAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moveSequence.Dequeue());
        _mockApiClient.Setup(c => c.MoveAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
            .Callback<Link, CancellationToken>((link, _) => linkCaptures.Add(link.Href))
            .ReturnsAsync(() => moveSequence.Dequeue());

        var solver = new HateoasSolver(_mockApiClient.Object, _settings, _mockLogger.Object);

        await solver.SolveAsync(maze);

        // Should choose first available unvisited (east in this case based on Directions order)
        Assert.Single(linkCaptures);
    }
```

**Step 2: Run tests to verify they pass**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "HateoasSolverTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 4

**Step 3: Commit**

```bash
git add tests/MazeOfHateoas.Solver.UnitTests/Services/HateoasSolverTests.cs
git commit -m "test(solver): add backtracking behavior tests"
```

---

## Task 13: SolverStats Service

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/SolverStats.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/SolverStatsTests.cs`

**Step 1: Write failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/SolverStatsTests.cs`:

```csharp
using MazeOfHateoas.Solver.Services;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class SolverStatsTests
{
    [Fact]
    public void Record_ShouldUpdateTotals()
    {
        var stats = new SolverStats();

        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 50, 1000, true));
        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 30, 500, true));

        Assert.Equal(2, stats.MazesSolved);
        Assert.Equal(80, stats.TotalMoves);
        Assert.Equal(40.0, stats.AverageMoves);
    }

    [Fact]
    public void Record_ShouldTrackFailures()
    {
        var stats = new SolverStats();

        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 10, 100, true));
        stats.Record(new SolveResult(Guid.NewGuid(), Guid.NewGuid(), 5, 50, false));

        Assert.Equal(1, stats.MazesSolved);
        Assert.Equal(1, stats.MazesFailed);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverStatsTests" --verbosity normal`

Expected: FAIL - type 'SolverStats' could not be found

**Step 3: Create SolverStats**

Create `src/MazeOfHateoas.Solver/Services/SolverStats.cs`:

```csharp
namespace MazeOfHateoas.Solver.Services;

public class SolverStats
{
    public int MazesSolved { get; private set; }
    public int MazesFailed { get; private set; }
    public long TotalMoves { get; private set; }
    public long TotalElapsedMs { get; private set; }

    public double AverageMoves => MazesSolved > 0 ? (double)TotalMoves / MazesSolved : 0;
    public double AverageElapsedMs => MazesSolved > 0 ? (double)TotalElapsedMs / MazesSolved : 0;

    public void Record(SolveResult result)
    {
        if (result.Success)
        {
            MazesSolved++;
            TotalMoves += result.MoveCount;
            TotalElapsedMs += result.ElapsedMs;
        }
        else
        {
            MazesFailed++;
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverStatsTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 2

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/SolverStats.cs tests/MazeOfHateoas.Solver.UnitTests/Services/SolverStatsTests.cs
git commit -m "feat(solver): add SolverStats for tracking metrics"
```

---

## Task 14: SolverHostedService

**Files:**
- Create: `src/MazeOfHateoas.Solver/Services/SolverHostedService.cs`
- Create: `tests/MazeOfHateoas.Solver.UnitTests/Services/SolverHostedServiceTests.cs`

**Step 1: Write failing test**

Create `tests/MazeOfHateoas.Solver.UnitTests/Services/SolverHostedServiceTests.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Models;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MazeOfHateoas.Solver.UnitTests.Services;

public class SolverHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateAndSolveMazes()
    {
        var mockApiClient = new Mock<IMazeApiClient>();
        var mockSolver = new Mock<ISolver>();
        var mockLogger = new Mock<ILogger<SolverHostedService>>();
        var settings = Options.Create(new SolverSettings
        {
            MazeWidth = 5,
            MazeHeight = 5,
            DelayBetweenMazesMs = 0,
            StatsIntervalMazes = 100
        });

        var maze = new MazeResponse
        {
            Id = Guid.NewGuid(),
            Width = 5,
            Height = 5,
            Start = new PositionDto { X = 0, Y = 0 },
            End = new PositionDto { X = 4, Y = 4 },
            CreatedAt = DateTime.UtcNow,
            Links = new Dictionary<string, Link>()
        };

        mockApiClient.Setup(c => c.CreateMazeAsync(5, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(maze);
        mockSolver.Setup(s => s.SolveAsync(maze, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolveResult(maze.Id, Guid.NewGuid(), 20, 500, true));

        var service = new SolverHostedService(
            mockApiClient.Object,
            mockSolver.Object,
            settings,
            mockLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        mockApiClient.Verify(c => c.CreateMazeAsync(5, 5, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockSolver.Verify(s => s.SolveAsync(maze, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverHostedServiceTests" --verbosity normal`

Expected: FAIL - type 'SolverHostedService' could not be found

**Step 3: Create SolverHostedService**

Create `src/MazeOfHateoas.Solver/Services/SolverHostedService.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Solver.Services;

public class SolverHostedService : BackgroundService
{
    private readonly IMazeApiClient _apiClient;
    private readonly ISolver _solver;
    private readonly SolverSettings _settings;
    private readonly ILogger<SolverHostedService> _logger;
    private readonly SolverStats _stats = new();

    public SolverHostedService(
        IMazeApiClient apiClient,
        ISolver solver,
        IOptions<SolverSettings> settings,
        ILogger<SolverHostedService> logger)
    {
        _apiClient = apiClient;
        _solver = solver;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Solver starting, connecting to {ApiBaseUrl}", _settings.ApiBaseUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var maze = await _apiClient.CreateMazeAsync(
                    _settings.MazeWidth,
                    _settings.MazeHeight,
                    stoppingToken);

                var result = await _solver.SolveAsync(maze, stoppingToken);
                _stats.Record(result);

                if ((_stats.MazesSolved + _stats.MazesFailed) % _settings.StatsIntervalMazes == 0)
                {
                    LogStats();
                }

                if (_settings.DelayBetweenMazesMs > 0)
                {
                    await Task.Delay(_settings.DelayBetweenMazesMs, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API request failed, retrying after delay");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error, retrying after delay");
                await Task.Delay(5000, stoppingToken);
            }
        }

        LogStats();
        _logger.LogInformation("Solver stopped");
    }

    private void LogStats()
    {
        _logger.LogInformation(
            "Stats: {MazesSolved} solved, {MazesFailed} failed, {TotalMoves} total moves, avg {AvgMoves:F1} moves/maze",
            _stats.MazesSolved,
            _stats.MazesFailed,
            _stats.TotalMoves,
            _stats.AverageMoves);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --filter "SolverHostedServiceTests" --verbosity normal`

Expected: Passed! - Failed: 0, Passed: 1

**Step 5: Commit**

```bash
git add src/MazeOfHateoas.Solver/Services/SolverHostedService.cs tests/MazeOfHateoas.Solver.UnitTests/Services/SolverHostedServiceTests.cs
git commit -m "feat(solver): add SolverHostedService for continuous solving"
```

---

## Task 15: Program.cs with DI and Serilog

**Files:**
- Modify: `src/MazeOfHateoas.Solver/Program.cs`

**Step 1: Update Program.cs**

Replace `src/MazeOfHateoas.Solver/Program.cs`:

```csharp
using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

try
{
    Log.Information("Starting Maze Solver");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    builder.Services.Configure<SolverSettings>(options =>
    {
        options.ApiBaseUrl = builder.Configuration["SOLVER_API_BASE_URL"] ?? "http://localhost:8080";
        options.MazeWidth = int.Parse(builder.Configuration["SOLVER_MAZE_WIDTH"] ?? "10");
        options.MazeHeight = int.Parse(builder.Configuration["SOLVER_MAZE_HEIGHT"] ?? "10");
        options.DelayBetweenMazesMs = int.Parse(builder.Configuration["SOLVER_DELAY_BETWEEN_MAZES_MS"] ?? "2000");
        options.DelayBetweenMovesMs = int.Parse(builder.Configuration["SOLVER_DELAY_BETWEEN_MOVES_MS"] ?? "0");
        options.StatsIntervalMazes = int.Parse(builder.Configuration["SOLVER_STATS_INTERVAL_MAZES"] ?? "10");
    });

    builder.Services.AddHttpClient<IMazeApiClient, MazeApiClient>((sp, client) =>
    {
        var config = builder.Configuration;
        client.BaseAddress = new Uri(config["SOLVER_API_BASE_URL"] ?? "http://localhost:8080");
    });

    builder.Services.AddSingleton<ISolver, HateoasSolver>();
    builder.Services.AddHostedService<SolverHostedService>();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

**Step 2: Verify build**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet build src/MazeOfHateoas.Solver/`

Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/MazeOfHateoas.Solver/Program.cs
git commit -m "feat(solver): add Program.cs with DI and Serilog"
```

---

## Task 16: Dockerfile.solver

**Files:**
- Create: `Dockerfile.solver`

**Step 1: Create Dockerfile.solver**

Create `Dockerfile.solver`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj", "src/MazeOfHateoas.Solver/"]
RUN dotnet restore "src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj"
COPY ["src/MazeOfHateoas.Solver/", "src/MazeOfHateoas.Solver/"]
RUN dotnet publish "src/MazeOfHateoas.Solver/MazeOfHateoas.Solver.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MazeOfHateoas.Solver.dll"]
```

**Step 2: Verify Docker build**

Run: `docker build -f Dockerfile.solver -t maze-solver .`

Expected: Successfully built

**Step 3: Commit**

```bash
git add Dockerfile.solver
git commit -m "feat(solver): add Dockerfile.solver"
```

---

## Task 17: docker-compose.solver.yml

**Files:**
- Create: `docker-compose.solver.yml`

**Step 1: Create docker-compose.solver.yml**

Create `docker-compose.solver.yml`:

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Serilog__MinimumLevel__Default=Information
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/api/mazes || exit 1"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 10s

  solver:
    build:
      context: .
      dockerfile: Dockerfile.solver
    depends_on:
      api:
        condition: service_healthy
    environment:
      - SOLVER_API_BASE_URL=http://api:8080
      - SOLVER_MAZE_WIDTH=10
      - SOLVER_MAZE_HEIGHT=10
      - SOLVER_DELAY_BETWEEN_MAZES_MS=2000
      - SOLVER_DELAY_BETWEEN_MOVES_MS=0
      - SOLVER_STATS_INTERVAL_MAZES=10
      - Serilog__MinimumLevel__Default=Information
```

**Step 2: Verify compose file syntax**

Run: `docker compose -f docker-compose.solver.yml config`

Expected: Valid YAML output

**Step 3: Commit**

```bash
git add docker-compose.solver.yml
git commit -m "feat(solver): add docker-compose.solver.yml for orchestration"
```

---

## Task 18: Clean Up Placeholder Test

**Files:**
- Delete: `tests/MazeOfHateoas.Solver.UnitTests/PlaceholderTests.cs`

**Step 1: Remove placeholder test**

Run: `rm tests/MazeOfHateoas.Solver.UnitTests/PlaceholderTests.cs`

**Step 2: Run all solver tests**

Run: `docker compose -f docker-compose.test.yml run --rm test dotnet test tests/MazeOfHateoas.Solver.UnitTests/ --verbosity normal`

Expected: All tests pass (should be 12+ tests)

**Step 3: Commit**

```bash
git add -u
git commit -m "chore(solver): remove placeholder test"
```

---

## Task 19: Run Full Test Suite

**Files:** None (verification only)

**Step 1: Run all tests**

Run: `docker compose -f docker-compose.test.yml up --build`

Expected: All tests pass (190+ original tests + new solver tests)

**Step 2: Verify no regressions**

Check output shows: `Passed!` for both unit and integration test projects.

---

## Task 20: Integration Test - Solver End-to-End

**Files:** None (manual verification)

**Step 1: Start the solver with API**

Run: `docker compose -f docker-compose.solver.yml up --build`

**Step 2: Observe output**

Expected:
- API starts and becomes healthy
- Solver connects and starts generating/solving mazes
- JSON log output shows maze completions
- Stats logged every 10 mazes

**Step 3: Stop with Ctrl+C**

Expected: Graceful shutdown with final stats logged

**Step 4: Clean up**

Run: `docker compose -f docker-compose.solver.yml down`

---

## Summary

This plan creates a complete maze solver console application with:

- **13 commits** building incrementally
- **12+ unit tests** covering all components
- **TDD workflow** throughout
- **Structured JSON logging** with configurable verbosity
- **Docker support** for standalone and orchestrated modes
- **Clean architecture** with proper DI and interfaces
