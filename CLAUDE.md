# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Directives

These all *must* be followed:
* Use Test Driven Development (TDD) **ALWAYS** write tests first
* Adhere to the tenets of the Pragmatic Programmer
* The system must be designed according to the 12-factor APP
* Adhere to the SOLID principle
* Builds, tests, etc **MUST** be run in a container rather than locally

## Commands

```bash
# Run the API
docker compose up --build

# Run all tests
docker compose -f docker-compose.test.yml up --build

# Run tests with filter (single test or class)
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~CellTests"
docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~CellTests.CanMove_ReturnsTrue"
```

Test results are output to `./TestResults/` as `.trx` files.

## Architecture

Clean Architecture with four layers (dependency flows inward):

```
API (Controllers, HATEOAS links)
  → Application (Interfaces, Services)
    → Domain (Entities, Value Objects, Enums)
      ← Infrastructure (implements Application interfaces)
```

**Domain Layer** (`src/MazeOfHateoas.Domain/`): Pure entities (`Maze`, `MazeSession`), value objects (`Position`, `Cell`), enums (`Direction`, `SessionState`, `MoveResult`). No dependencies.

**Application Layer** (`src/MazeOfHateoas.Application/`): Interfaces for `IMazeRepository`, `ISessionRepository`, `IMazeGenerator`. Defines contracts that Infrastructure implements.

**Infrastructure Layer** (`src/MazeOfHateoas.Infrastructure/`): In-memory implementations using `ConcurrentDictionary`. `MazeGenerator` uses recursive backtracking algorithm.

**API Layer** (`src/MazeOfHateoas.Api/`): REST controllers, HATEOAS link generation (`SessionLinkGenerator`), response models with `_links` dictionaries.

## Key Concepts

**HATEOAS Links**: Responses include `_links` dictionary with available actions based on state. Session responses dynamically include only movement directions where no wall exists.

**Maze Generation**: Uses depth-first search with backtracking. Start at (0,0), end at (width-1, height-1). Seeded random for deterministic testing.

**Configuration**: Environment variables: `MAZE_DEFAULT_WIDTH`, `MAZE_DEFAULT_HEIGHT`, `MAZE_MAX_WIDTH`, `MAZE_MAX_HEIGHT`, `Serilog__MinimumLevel__Default`.

## Test Organization

- `tests/MazeOfHateoas.UnitTests/`: Domain logic, services, controllers
- `tests/MazeOfHateoas.IntegrationTests/`: Full HTTP request/response testing with `WebApplicationFactory`

## Documentation

- `docs/DESIGN.md`: Full design documentation with API examples and algorithms
