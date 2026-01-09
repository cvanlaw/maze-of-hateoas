# PBI-03: Generate Maze - Full Vertical Slice

## Status: Ready

## Description

Deliver the first working API endpoint: `POST /api/mazes`. This vertical slice includes the Maze entity, maze generation algorithm, in-memory persistence, and API controller. A client can create a maze and receive a HATEOAS response with links to view it or start a session.

## Acceptance Criteria

- [ ] `Maze` entity in Domain layer with:
  - [ ] Properties: `Id` (Guid), `Width`, `Height`, `Cells` (Cell[,]), `Start`, `End` (Position), `CreatedAt`
- [ ] `IMazeGenerator` interface in Application layer
- [ ] `MazeGenerator` implementation in Infrastructure using recursive backtracking:
  - [ ] Accepts optional `Random` for deterministic testing
  - [ ] Generates valid, solvable mazes (path exists from start to end)
  - [ ] Start at (0,0), End at (Width-1, Height-1)
- [ ] `IMazeRepository` interface in Application layer with: `GetByIdAsync`, `GetAllAsync`, `SaveAsync`
- [ ] `InMemoryMazeRepository` in Infrastructure using `ConcurrentDictionary`
- [ ] `MazesController` in API layer with `POST /api/mazes` endpoint
- [ ] Request body accepts `width` and `height` (optional, defaults from config)
- [ ] Response (201 Created) includes:
  - [ ] Maze properties: `id`, `width`, `height`, `start`, `end`, `createdAt`
  - [ ] `_links.self` pointing to `GET /api/mazes/{id}`
  - [ ] `_links.start` pointing to `POST /api/mazes/{id}/sessions`
- [ ] `MazeSettings` configuration class bound from environment variables
- [ ] Unit tests for maze generator (seeded random produces expected maze)
- [ ] Integration test: POST creates maze, returns 201 with correct HATEOAS links
- [ ] All tests pass in Docker container

## Dependencies

- PBI-02 (Domain Core Value Objects)

## Technical Notes

### Recursive Backtracking Algorithm
```
1. Create grid with all walls intact
2. Start at (0,0), mark visited, push to stack
3. While stack not empty:
   a. Pop current cell
   b. Get unvisited neighbors
   c. If neighbors exist:
      - Push current back to stack
      - Choose random neighbor
      - Remove wall between current and neighbor
      - Mark neighbor visited, push to stack
```

### HATEOAS Response Example
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "width": 10,
  "height": 10,
  "start": { "x": 0, "y": 0 },
  "end": { "x": 9, "y": 9 },
  "createdAt": "2025-01-09T10:30:00Z",
  "_links": {
    "self": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET" },
    "start": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST" }
  }
}
```

### Configuration (Environment Variables)
- `MAZE_DEFAULT_WIDTH` (default: 10)
- `MAZE_DEFAULT_HEIGHT` (default: 10)

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual API test (after PBI-08 or with local run)
curl -X POST http://localhost:8080/api/mazes \
  -H "Content-Type: application/json" \
  -d '{"width": 5, "height": 5}'
```
