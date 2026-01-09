# PBI-05: Start Session - Vertical Slice

## Status: Ready

## Description

Deliver the session creation endpoint: `POST /api/mazes/{mazeId}/sessions`. This allows a player to "enter" a maze and begin navigation. The response shows the starting position and available moves as HATEOAS links.

## Acceptance Criteria

- [ ] `SessionState` enum in Domain: `InProgress`, `Completed`
- [ ] `MazeSession` entity in Domain with:
  - [ ] Properties: `Id`, `MazeId`, `CurrentPosition`, `State`, `StartedAt`
  - [ ] Constructor sets `CurrentPosition` to maze's `Start` position
  - [ ] Constructor sets `State` to `InProgress`
- [ ] `ISessionRepository` interface in Application with: `GetByIdAsync`, `GetByMazeIdAsync`, `SaveAsync`
- [ ] `InMemorySessionRepository` in Infrastructure
- [ ] `SessionsController` in API layer (nested under mazes)
- [ ] `POST /api/mazes/{mazeId}/sessions` endpoint:
  - [ ] Creates new session for specified maze
  - [ ] Returns 201 Created with Location header
  - [ ] Returns 404 if maze not found
- [ ] Response includes:
  - [ ] Session properties: `id`, `mazeId`, `currentPosition`, `state`, `startedAt`
  - [ ] `_links.self` pointing to GET session
  - [ ] `_links.{direction}` for each available move from start position (N/S/E/W based on walls)
- [ ] Integration test: Create maze, start session, verify position is at start
- [ ] Integration test: Start session on non-existent maze returns 404
- [ ] Integration test: Session response includes only valid move links (no links for blocked directions)
- [ ] All tests pass in Docker container

## Dependencies

- PBI-03 (Generate Maze)

## Technical Notes

### Session Creation Response
```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 0, "y": 0 },
  "state": "InProgress",
  "startedAt": "2025-01-09T10:35:00Z",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "south": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/south", "rel": "move", "method": "POST" },
    "east": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/east", "rel": "move", "method": "POST" }
  }
}
```

Note: At position (0,0), north and west are always blocked by maze boundary. South and east links only appear if those walls are open.

### Key HATEOAS Behavior
The move links in the response are **dynamic** based on:
1. Current position in maze
2. Which walls exist at that cell
3. Maze boundaries

This is the core HATEOAS principle - the client doesn't need to know the maze layout, it just follows available links.

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual verification
MAZE_ID=$(curl -s -X POST http://localhost:8080/api/mazes -H "Content-Type: application/json" -d '{}' | jq -r '.id')
curl -X POST http://localhost:8080/api/mazes/$MAZE_ID/sessions
```
