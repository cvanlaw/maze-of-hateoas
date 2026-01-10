# Structured JSON Logging Design

## Overview

Add structured JSON logging to the MazeOfHateoas API using Serilog with configurable log levels via appsettings.json and environment variable overrides.

## Requirements

- Structured JSON output to console/stdout
- Configurable log levels (appsettings.json with env var override)
- Log HTTP requests/responses with correlation IDs
- Log application events (maze created, session started, moves, completion)
- Log errors/exceptions with full context
- Standard metadata: timestamp, level, source context, environment, request ID

## Packages

| Package | Purpose |
|---------|---------|
| `Serilog.AspNetCore` | Core ASP.NET Core integration |
| `Serilog.Formatting.Compact` | Compact JSON formatter |

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### Environment Variable Override

```bash
Serilog__MinimumLevel__Default=Warning
```

### Log Levels (Serilog)

| Level | Usage |
|-------|-------|
| `Verbose` | Detailed tracing (rarely used) |
| `Debug` | Development diagnostics |
| `Information` | Business events, request logging |
| `Warning` | Client errors (4xx), blocked moves |
| `Error` | Exceptions, server errors (5xx) |
| `Fatal` | Application crashes |

## Implementation

### Program.cs - Serilog Setup

```csharp
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Replace default logging with Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MazeOfHateoas")
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

### Program.cs - Request Logging Middleware

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };

    // Filter out health check noise
    options.GetLevel = (httpContext, elapsed, ex) =>
        httpContext.Request.Path == "/health"
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
});
```

### Program.cs - Exception Handler Logging

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        // Existing Problem Details response...
    });
});
```

### Controller Logging

**MazesController.cs:**
```csharp
public class MazesController : ControllerBase
{
    private readonly ILogger<MazesController> _logger;

    public MazesController(..., ILogger<MazesController> logger)
    {
        _logger = logger;
    }

    // POST /api/mazes
    _logger.LogInformation("Maze created: {MazeId} ({Width}x{Height})",
        maze.Id, request.Width, request.Height);

    // GET /api/mazes/{id} - not found
    _logger.LogWarning("Maze not found: {MazeId}", id);

    // POST /api/mazes/{id}/sessions
    _logger.LogInformation("Session started: {SessionId} for maze {MazeId}",
        session.Id, mazeId);
}
```

**SessionsController.cs:**
```csharp
public class SessionsController : ControllerBase
{
    private readonly ILogger<SessionsController> _logger;

    // GET /api/mazes/{mazeId}/sessions/{sessionId} - not found
    _logger.LogWarning("Session not found: {SessionId}", sessionId);

    // POST .../move/{direction}
    _logger.LogInformation("Move {Direction}: {Result} for session {SessionId}",
        direction, result, sessionId);

    // On completion
    _logger.LogInformation("Session {SessionId} completed maze {MazeId}",
        sessionId, mazeId);
}
```

## Log Output Format

**Request log:**
```json
{"@t":"2026-01-09T12:00:00.000Z","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms","RequestMethod":"POST","RequestPath":"/api/mazes","StatusCode":201,"Elapsed":45.2,"RequestId":"abc-123","Application":"MazeOfHateoas","Environment":"Production"}
```

**Application event:**
```json
{"@t":"2026-01-09T12:00:00.100Z","@mt":"Maze created: {MazeId} ({Width}x{Height})","MazeId":"550e8400-e29b-41d4-a716-446655440000","Width":10,"Height":10,"SourceContext":"MazeOfHateoas.Api.Controllers.MazesController"}
```

**Error:**
```json
{"@t":"2026-01-09T12:00:00.200Z","@l":"Error","@mt":"Unhandled exception for {Method} {Path}","Method":"POST","Path":"/api/mazes","@x":"System.ArgumentException: Invalid maze dimensions...","SourceContext":"Program"}
```

## Files to Modify

1. `src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj` - Add NuGet packages
2. `src/MazeOfHateoas.Api/Program.cs` - Serilog setup, middleware, exception logging
3. `src/MazeOfHateoas.Api/Controllers/MazesController.cs` - Inject and use ILogger
4. `src/MazeOfHateoas.Api/Controllers/SessionsController.cs` - Inject and use ILogger
5. `src/MazeOfHateoas.Api/appsettings.json` - Add Serilog config section
6. `src/MazeOfHateoas.Api/appsettings.Development.json` - Debug level default

## Testing Considerations

- Unit tests: Mock `ILogger<T>` (or use `NullLogger<T>`)
- Integration tests: Serilog writes to console, captured by test output if needed
- No functional changes to API behavior - logging is observability only

## Docker Usage

```yaml
# docker-compose.yml
environment:
  - Serilog__MinimumLevel__Default=Information

# Production override
environment:
  - Serilog__MinimumLevel__Default=Warning
```
