# PBI-09: Error Handling and Validation

## Status: Ready

## Description

Implement comprehensive error handling and input validation across all endpoints. All errors return RFC 7807 Problem Details format. This ensures the API is robust and provides clear feedback for invalid requests.

## Acceptance Criteria

- [ ] Global exception handler middleware returns Problem Details for unhandled exceptions
- [ ] `POST /api/mazes` validates dimensions:
  - [ ] Width and Height must be positive integers
  - [ ] Width and Height must not exceed `MAZE_MAX_WIDTH` / `MAZE_MAX_HEIGHT`
  - [ ] Invalid dimensions return 400 with descriptive Problem Details
- [ ] All 404 responses use Problem Details with:
  - [ ] `type`: URI reference for error type
  - [ ] `title`: Short description
  - [ ] `status`: 404
  - [ ] `detail`: Specific message (e.g., "Maze with ID 'xyz' was not found")
  - [ ] `instance`: Request path
- [ ] All 400 responses use Problem Details with validation errors
- [ ] Invalid direction in move endpoint returns 400 (not 404)
- [ ] Integration test: Create maze with invalid dimensions, verify 400 response
- [ ] Integration test: Create maze with dimensions exceeding max, verify 400 response
- [ ] Integration test: Verify all error responses conform to Problem Details schema
- [ ] All tests pass in Docker container

## Dependencies

- PBI-07 (Move Through Maze)

## Technical Notes

### Problem Details (RFC 7807)
ASP.NET Core has built-in support for Problem Details:

```csharp
// Program.cs
builder.Services.AddProblemDetails();

// Controller
return Problem(
    detail: "Maze with ID 'xyz' was not found",
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found",
    instance: HttpContext.Request.Path
);
```

### Global Exception Handler
```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

### Validation Examples

**Invalid Dimensions (negative):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Width must be a positive integer",
  "instance": "/api/mazes"
}
```

**Dimensions Exceed Maximum:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Width cannot exceed 50",
  "instance": "/api/mazes"
}
```

**Invalid Direction:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid direction 'up'. Valid directions are: north, south, east, west",
  "instance": "/api/mazes/{mazeId}/sessions/{sessionId}/move/up"
}
```

### Validation with Data Annotations
```csharp
public class CreateMazeRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Width must be positive")]
    public int? Width { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Height must be positive")]
    public int? Height { get; set; }
}
```

### Custom Validation for Max Dimensions
```csharp
public class MazeValidationService
{
    private readonly MazeSettings _settings;

    public ValidationResult ValidateDimensions(int width, int height)
    {
        if (width > _settings.MaxWidth)
            return ValidationResult.Failure($"Width cannot exceed {_settings.MaxWidth}");
        if (height > _settings.MaxHeight)
            return ValidationResult.Failure($"Height cannot exceed {_settings.MaxHeight}");
        return ValidationResult.Success();
    }
}
```

## Verification

```bash
# Run tests
docker compose -f docker-compose.test.yml up --build

# Manual validation tests
# Invalid dimensions
curl -X POST http://localhost:8080/api/mazes \
  -H "Content-Type: application/json" \
  -d '{"width": -1, "height": 10}'

# Exceeds maximum
curl -X POST http://localhost:8080/api/mazes \
  -H "Content-Type: application/json" \
  -d '{"width": 100, "height": 10}'

# Invalid direction
curl -X POST http://localhost:8080/api/mazes/$MAZE_ID/sessions/$SESSION_ID/move/up

# All should return Problem Details JSON with appropriate status codes
```
