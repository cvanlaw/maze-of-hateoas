---
id: 2
title: Create Application and Infrastructure Projects
depends_on: [1]
status: pending
---

# Task 2: Create Application and Infrastructure Projects

## Description

Create the Application layer (references Domain) and Infrastructure layer (references Application) projects, establishing the middle tiers of the Clean Architecture.

## Deliverables

- `src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj` - Application project
- `src/MazeOfHateoas.Infrastructure/MazeOfHateoas.Infrastructure.csproj` - Infrastructure project

## Acceptance Criteria

- [ ] `src/MazeOfHateoas.Application/` project created with .NET 8
- [ ] Application project references Domain project only
- [ ] `src/MazeOfHateoas.Infrastructure/` project created with .NET 8
- [ ] Infrastructure project references Application project
- [ ] Both projects added to solution
- [ ] Solution builds successfully

## Implementation Details

### Commands

```bash
# Create Application project
dotnet new classlib -n MazeOfHateoas.Application -o src/MazeOfHateoas.Application -f net8.0

# Add Domain reference to Application
dotnet add src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj reference src/MazeOfHateoas.Domain/MazeOfHateoas.Domain.csproj

# Create Infrastructure project
dotnet new classlib -n MazeOfHateoas.Infrastructure -o src/MazeOfHateoas.Infrastructure -f net8.0

# Add Application reference to Infrastructure
dotnet add src/MazeOfHateoas.Infrastructure/MazeOfHateoas.Infrastructure.csproj reference src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj

# Add to solution
dotnet sln add src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj
dotnet sln add src/MazeOfHateoas.Infrastructure/MazeOfHateoas.Infrastructure.csproj
```

### Project References

```
Domain (no deps)
   ↑
Application (refs Domain)
   ↑
Infrastructure (refs Application)
```

## Testing Checklist

- [ ] Run `dotnet build MazeOfHateoas.sln` - should succeed
- [ ] Verify Application.csproj has `<ProjectReference>` to Domain only
- [ ] Verify Infrastructure.csproj has `<ProjectReference>` to Application only
