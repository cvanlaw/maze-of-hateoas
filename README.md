# Maze of HATEOAS

A RESTful API demonstrating Hypermedia as the Engine of Application State (HATEOAS) through interactive maze navigation. Players generate mazes and navigate by following hypermedia links—the API dynamically communicates available moves based on the player's position and surrounding walls.

## Quick Start

```bash
# Run the API
docker compose up --build

# Run tests
docker compose -f docker-compose.test.yml up --build
```

The API is available at `http://localhost:8080`.

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/mazes` | GET | List all mazes |
| `/api/mazes` | POST | Create a new maze |
| `/api/mazes/{id}` | GET | Get maze details |
| `/api/mazes/{id}/sessions` | POST | Start a navigation session |
| `/api/mazes/{id}/sessions/{sessionId}` | GET | Get current session state |
| `/api/mazes/{id}/sessions/{sessionId}/move/{direction}` | POST | Move in a direction |
| `/health` | GET | Health check |

## HATEOAS in Action

Responses include `_links` that tell you what actions are available:

```json
{
  "id": "session-guid",
  "currentPosition": { "x": 2, "y": 3 },
  "state": "InProgress",
  "_links": {
    "self": { "href": "/api/mazes/{id}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "north": { "href": "/api/mazes/{id}/sessions/{sessionId}/move/north", "rel": "move", "method": "POST" },
    "east": { "href": "/api/mazes/{id}/sessions/{sessionId}/move/east", "rel": "move", "method": "POST" }
  }
}
```

Only `north` and `east` links appear because walls block south and west. Follow the links to navigate—no maze knowledge required.

## Configuration

Environment variables (all optional):

| Variable | Description | Default |
|----------|-------------|---------|
| `MAZE_DEFAULT_WIDTH` | Default maze width | 10 |
| `MAZE_DEFAULT_HEIGHT` | Default maze height | 10 |
| `MAZE_MAX_WIDTH` | Maximum allowed width | 50 |
| `MAZE_MAX_HEIGHT` | Maximum allowed height | 50 |

## Project Structure

```
maze-of-hateoas/
├── src/
│   ├── MazeOfHateoas.Api/           # REST API layer
│   ├── MazeOfHateoas.Application/   # Business logic
│   ├── MazeOfHateoas.Domain/        # Core entities
│   └── MazeOfHateoas.Infrastructure/# Data persistence
├── tests/
│   ├── MazeOfHateoas.UnitTests/
│   └── MazeOfHateoas.IntegrationTests/
├── docs/
│   └── DESIGN.md                    # Full design documentation
├── Dockerfile
├── docker-compose.yml
└── docker-compose.test.yml
```

## Design Principles

- **TDD**: All code written test-first
- **Clean Architecture**: Domain-centric with dependency inversion
- **12-Factor App**: Configuration via environment variables
- **SOLID**: Single responsibility, open/closed, dependency injection

See [docs/DESIGN.md](docs/DESIGN.md) for detailed design documentation.
