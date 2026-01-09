# PBI-04: List and Get Mazes

## Status: Ready

## Description

Add endpoints to list all mazes and retrieve a specific maze by ID. These endpoints complete the maze resource CRUD operations (Create from PBI-03, Read here). HATEOAS links guide clients to available actions.

## Acceptance Criteria

- [ ] `GET /api/mazes` endpoint returns:
  - [ ] Array of maze summaries (id, width, height, createdAt)
  - [ ] Each maze includes `_links.self` and `_links.start`
  - [ ] Collection includes `_links.self` and `_links.create`
- [ ] `GET /api/mazes/{mazeId}` endpoint returns:
  - [ ] Full maze details (id, width, height, start, end, createdAt)
  - [ ] `_links.self` and `_links.start`
- [ ] `GET /api/mazes/{mazeId}` returns 404 with Problem Details when maze not found
- [ ] Integration test: Create maze, list mazes, verify created maze appears
- [ ] Integration test: Create maze, get by ID, verify properties match
- [ ] Integration test: Get non-existent maze returns 404 Problem Details
- [ ] All tests pass in Docker container

## Dependencies

- PBI-03 (Generate Maze)

## Technical Notes

### GET /api/mazes Response
```json
{
  "mazes": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "width": 10,
      "height": 10,
      "createdAt": "2025-01-09T10:30:00Z",
      "_links": {
        "self": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000", "rel": "self", "method": "GET" },
        "start": { "href": "/api/mazes/550e8400-e29b-41d4-a716-446655440000/sessions", "rel": "start", "method": "POST" }
      }
    }
  ],
  "_links": {
    "self": { "href": "/api/mazes", "rel": "self", "method": "GET" },
    "create": { "href": "/api/mazes", "rel": "create", "method": "POST" }
  }
}
```

### 404 Problem Details Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Maze with ID '123' was not found",
  "instance": "/api/mazes/123"
}
```

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual verification
curl http://localhost:8080/api/mazes
curl http://localhost:8080/api/mazes/{id}
curl http://localhost:8080/api/mazes/nonexistent-id  # Should return 404
```
