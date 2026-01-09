# PBI-06: Get Session State with Dynamic Links

## Status: Ready

## Description

Add the endpoint to retrieve current session state: `GET /api/mazes/{mazeId}/sessions/{sessionId}`. The key HATEOAS feature is that move links dynamically reflect which directions are actually available from the current position.

## Acceptance Criteria

- [ ] `GET /api/mazes/{mazeId}/sessions/{sessionId}` endpoint returns:
  - [ ] Session properties: `id`, `mazeId`, `currentPosition`, `state`
  - [ ] `_links.self`
  - [ ] `_links.north` only if north is not blocked by wall
  - [ ] `_links.south` only if south is not blocked by wall
  - [ ] `_links.east` only if east is not blocked by wall
  - [ ] `_links.west` only if west is not blocked by wall
- [ ] Returns 404 with Problem Details if session not found
- [ ] Returns 404 with Problem Details if maze not found
- [ ] Link generation logic encapsulated in reusable helper/service
- [ ] Integration test: Create session, get state, verify links match cell walls
- [ ] Integration test: Get non-existent session returns 404
- [ ] Unit test: Link generator produces correct links for various cell configurations
- [ ] All tests pass in Docker container

## Dependencies

- PBI-05 (Start Session)

## Technical Notes

### Dynamic Link Generation
The link generation service should:
1. Get the cell at the session's current position
2. Check each direction for walls
3. Only include link if direction is passable

```csharp
public interface ISessionLinkGenerator
{
    Dictionary<string, Link> GenerateLinks(MazeSession session, Maze maze, HttpContext context);
}
```

### Response Examples

**At position (2,3) with walls blocking South and West:**
```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 2, "y": 3 },
  "state": "InProgress",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "north": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/north", "rel": "move", "method": "POST" },
    "east": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/east", "rel": "move", "method": "POST" }
  }
}
```

**At corner (0,0) - only south/east possible at most:**
```json
{
  "_links": {
    "self": { "href": "...", "rel": "self", "method": "GET" },
    "east": { "href": "...", "rel": "move", "method": "POST" }
  }
}
```

### Link Generation Logic
```csharp
var cell = maze.Cells[session.CurrentPosition.X, session.CurrentPosition.Y];

if (cell.CanMove(Direction.North) && session.CurrentPosition.Y > 0)
    links["north"] = CreateMoveLink(Direction.North);
// ... repeat for other directions
```

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual verification - create session then get state
SESSION_RESPONSE=$(curl -s -X POST http://localhost:8080/api/mazes/$MAZE_ID/sessions)
SESSION_ID=$(echo $SESSION_RESPONSE | jq -r '.id')
curl http://localhost:8080/api/mazes/$MAZE_ID/sessions/$SESSION_ID
```
