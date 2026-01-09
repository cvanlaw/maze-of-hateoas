---
id: 3
title: Create Api and Test Projects
depends_on: [2]
status: pending
---

# Task 3: Create Api and Test Projects

## Description

Create the API project (outermost layer) and both test projects. Include a placeholder test that passes to verify the test infrastructure works.

## Deliverables

- `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj` - Web API project
- `tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj` - Unit test project
- `tests/MazeOfHateoas.IntegrationTests/MazeOfHateoas.IntegrationTests.csproj` - Integration test project

## Acceptance Criteria

- [ ] `src/MazeOfHateoas.Api/` project created as ASP.NET Core Web API
- [ ] Api project references Application and Infrastructure projects
- [ ] `tests/MazeOfHateoas.UnitTests/` project created with xUnit
- [ ] UnitTests project references Domain and Application projects
- [ ] `tests/MazeOfHateoas.IntegrationTests/` project created with xUnit
- [ ] IntegrationTests project references Api project
- [ ] At least one placeholder test exists and passes
- [ ] All projects added to solution
- [ ] `dotnet test` executes successfully with passing tests

## Implementation Details

### Commands

```bash
# Create Api project
dotnet new webapi -n MazeOfHateoas.Api -o src/MazeOfHateoas.Api -f net8.0

# Add references to Api
dotnet add src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj reference src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj
dotnet add src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj reference src/MazeOfHateoas.Infrastructure/MazeOfHateoas.Infrastructure.csproj

# Create UnitTests project
dotnet new xunit -n MazeOfHateoas.UnitTests -o tests/MazeOfHateoas.UnitTests -f net8.0

# Add references to UnitTests
dotnet add tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj reference src/MazeOfHateoas.Domain/MazeOfHateoas.Domain.csproj
dotnet add tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj reference src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj

# Create IntegrationTests project
dotnet new xunit -n MazeOfHateoas.IntegrationTests -o tests/MazeOfHateoas.IntegrationTests -f net8.0

# Add reference to IntegrationTests
dotnet add tests/MazeOfHateoas.IntegrationTests/MazeOfHateoas.IntegrationTests.csproj reference src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj

# Add all to solution
dotnet sln add src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj
dotnet sln add tests/MazeOfHateoas.UnitTests/MazeOfHateoas.UnitTests.csproj
dotnet sln add tests/MazeOfHateoas.IntegrationTests/MazeOfHateoas.IntegrationTests.csproj
```

### Placeholder Test

The xUnit template includes a placeholder test. Verify it exists or create one:

```csharp
// tests/MazeOfHateoas.UnitTests/PlaceholderTests.cs
namespace MazeOfHateoas.UnitTests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        Assert.True(true);
    }
}
```

### Project Reference Summary

```
Domain (no deps)
   ↑
Application (refs Domain)
   ↑                    ↑
Infrastructure    UnitTests (refs Domain, Application)
   ↑
   Api (refs Application, Infrastructure)
   ↑
IntegrationTests (refs Api)
```

## Testing Checklist

- [ ] Run `dotnet build MazeOfHateoas.sln` - should succeed
- [ ] Run `dotnet test` - should find and pass at least one test
- [ ] Verify Api.csproj references Application and Infrastructure
- [ ] Verify UnitTests.csproj references Domain and Application
- [ ] Verify IntegrationTests.csproj references Api
