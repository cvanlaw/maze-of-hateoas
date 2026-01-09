# PBI-07: Move Through Maze

## Status: Ready

## Description

Implement the core gameplay endpoint: `POST /api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}`. Players navigate by following HATEOAS links. The system detects when the player reaches the end and marks the session complete.

## Acceptance Criteria

- [ ] `MoveResult` enum in Domain: `Success`, `Blocked`, `OutOfBounds`, `AlreadyCompleted`
- [ ] `MazeSession.Move(Direction, Maze)` method:
  - [ ] Returns `AlreadyCompleted` if session state is `Completed`
  - [ ] Returns `Blocked` if wall exists in that direction
  - [ ] Returns `OutOfBounds` if move would exit maze bounds
  - [ ] Returns `Success` and updates `CurrentPosition` on valid move
  - [ ] Sets `State` to `Completed` if new position equals maze `End`
- [ ] `POST /api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}` endpoint:
  - [ ] Accepts direction as route parameter (north/south/east/west)
  - [ ] Returns updated session state with new position
  - [ ] Returns 400 Bad Request with Problem Details if move blocked
  - [ ] Returns 400 Bad Request if session already completed
  - [ ] Returns 404 if session or maze not found
- [ ] Response includes `moveResult` field indicating outcome
- [ ] When session completes:
  - [ ] `state` changes to `Completed`
  - [ ] `message` field shows congratulations text
  - [ ] Move links are replaced with `_links.mazes` and `_links.newMaze`
- [ ] Integration test: Navigate from start to end, verify completion
- [ ] Integration test: Attempt blocked move, verify 400 response
- [ ] Integration test: Attempt move on completed session, verify 400 response
- [ ] Unit tests for `MazeSession.Move()` covering all result types
- [ ] All tests pass in Docker container

## Dependencies

- PBI-06 (Get Session State)

## Technical Notes

### Move Endpoint Route
```
POST /api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}
```
Where `direction` is one of: `north`, `south`, `east`, `west`

### Successful Move Response
```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 2, "y": 2 },
  "state": "InProgress",
  "moveResult": "Success",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "south": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}/move/south", "rel": "move", "method": "POST" }
  }
}
```

### Completion Response
```json
{
  "id": "session-guid",
  "mazeId": "maze-guid",
  "currentPosition": { "x": 9, "y": 9 },
  "state": "Completed",
  "moveResult": "Success",
  "message": "Congratulations! You've completed the maze!",
  "_links": {
    "self": { "href": "/api/mazes/{mazeId}/sessions/{sessionId}", "rel": "self", "method": "GET" },
    "mazes": { "href": "/api/mazes", "rel": "collection", "method": "GET" },
    "newMaze": { "href": "/api/mazes", "rel": "create", "method": "POST" }
  }
}
```

### Blocked Move Response (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Cannot move north - blocked by wall",
  "instance": "/api/mazes/{mazeId}/sessions/{sessionId}/move/north"
}
```

### MazeSession.Move Logic
```csharp
public MoveResult Move(Direction direction, Maze maze)
{
    if (State == SessionState.Completed)
        return MoveResult.AlreadyCompleted;

    var cell = maze.Cells[CurrentPosition.X, CurrentPosition.Y];
    if (!cell.CanMove(direction))
        return MoveResult.Blocked;

    var newPosition = CurrentPosition.Move(direction);
    if (!IsWithinBounds(newPosition, maze))
        return MoveResult.OutOfBounds;

    CurrentPosition = newPosition;

    if (CurrentPosition == maze.End)
        State = SessionState.Completed;

    return MoveResult.Success;
}
```

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual verification - navigate through maze
# Follow the HATEOAS links from session state
curl -X POST http://localhost:8080/api/mazes/$MAZE_ID/sessions/$SESSION_ID/move/east
```
