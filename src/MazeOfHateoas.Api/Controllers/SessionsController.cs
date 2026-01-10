using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MazeOfHateoas.Api.Controllers;

/// <summary>
/// Controller for managing maze navigation sessions.
/// </summary>
/// <remarks>
/// Sessions track a player's progress through a maze. Each session maintains
/// the current position and state, and provides HATEOAS links indicating
/// valid moves based on the maze layout and walls.
/// </remarks>
[ApiController]
[Route("api/mazes/{mazeId}/sessions")]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly IMazeRepository _mazeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionLinkGenerator _linkGenerator;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        IMazeRepository mazeRepository,
        ISessionRepository sessionRepository,
        ISessionLinkGenerator linkGenerator,
        ILogger<SessionsController> logger)
    {
        _mazeRepository = mazeRepository;
        _sessionRepository = sessionRepository;
        _linkGenerator = linkGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new navigation session for a maze.
    /// </summary>
    /// <remarks>
    /// Starts a new session at the maze's start position (0,0).
    /// The response includes HATEOAS links for available movement directions.
    /// </remarks>
    /// <param name="mazeId">The unique identifier of the maze to navigate.</param>
    /// <returns>The created session with current position and available moves.</returns>
    /// <response code="201">Returns the newly created session.</response>
    /// <response code="404">If the maze with the specified ID was not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> CreateSession(Guid mazeId)
    {
        var maze = await _mazeRepository.GetByIdAsync(mazeId);

        if (maze == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Maze with ID '{mazeId}' was not found",
                Instance = $"/api/mazes/{mazeId}/sessions"
            });
        }

        var session = new MazeSession(Guid.NewGuid(), mazeId, maze.Start);
        await _sessionRepository.SaveAsync(session);

        _logger.LogInformation("Session started: {SessionId} for maze {MazeId}",
            session.Id, mazeId);

        var response = BuildSessionResponse(session, maze);

        return CreatedAtAction(
            nameof(GetSession),
            new { mazeId = session.MazeId, sessionId = session.Id },
            response);
    }

    /// <summary>
    /// Retrieves the current state of a navigation session.
    /// </summary>
    /// <remarks>
    /// Returns the session's current position, state, and HATEOAS links
    /// for available movement directions based on the maze walls.
    /// </remarks>
    /// <param name="mazeId">The unique identifier of the maze.</param>
    /// <param name="sessionId">The unique identifier of the session.</param>
    /// <returns>The session state with available moves.</returns>
    /// <response code="200">Returns the session state.</response>
    /// <response code="404">If the maze or session was not found.</response>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> GetSession(Guid mazeId, Guid sessionId)
    {
        var maze = await _mazeRepository.GetByIdAsync(mazeId);
        if (maze == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Maze with ID '{mazeId}' was not found",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}"
            });
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.MazeId != mazeId)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Session with ID '{sessionId}' was not found",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}"
            });
        }

        var response = BuildSessionResponse(session, maze);
        return Ok(response);
    }

    /// <summary>
    /// Moves the player in the specified direction.
    /// </summary>
    /// <remarks>
    /// Attempts to move the player in the given direction. The move will fail if:
    /// - The direction is blocked by a wall
    /// - The move would go out of bounds
    /// - The session is already completed
    ///
    /// Valid directions are: north, south, east, west (case-insensitive).
    ///
    /// Upon reaching the end position, the session state changes to "Completed".
    /// </remarks>
    /// <param name="mazeId">The unique identifier of the maze.</param>
    /// <param name="sessionId">The unique identifier of the session.</param>
    /// <param name="direction">The direction to move (north, south, east, west).</param>
    /// <returns>The updated session state with new position and available moves.</returns>
    /// <response code="200">Move was successful. Returns updated session state.</response>
    /// <response code="400">If the direction is invalid, move is blocked, or session is completed.</response>
    /// <response code="404">If the maze or session was not found.</response>
    [HttpPost("{sessionId}/move/{direction}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Move(Guid mazeId, Guid sessionId, string direction)
    {
        if (!TryParseDirection(direction, out var parsedDirection))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = $"Invalid direction '{direction}'. Valid directions are: north, south, east, west",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        var maze = await _mazeRepository.GetByIdAsync(mazeId);
        if (maze == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Maze with ID '{mazeId}' was not found",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.MazeId != mazeId)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Session with ID '{sessionId}' was not found",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        var moveResult = session.Move(parsedDirection, maze);

        if (moveResult == MoveResult.AlreadyCompleted)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = "Cannot move - session is already completed",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        if (moveResult == MoveResult.Blocked)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = $"Cannot move {direction} - blocked by wall",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        if (moveResult == MoveResult.OutOfBounds)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = $"Cannot move {direction} - out of bounds",
                Instance = $"/api/mazes/{mazeId}/sessions/{sessionId}/move/{direction}"
            });
        }

        await _sessionRepository.SaveAsync(session);

        _logger.LogInformation("Move {Direction}: {Result} for session {SessionId}",
            direction, moveResult, sessionId);

        if (session.State == SessionState.Completed)
        {
            _logger.LogInformation("Session {SessionId} completed maze {MazeId}",
                sessionId, mazeId);
        }

        var response = BuildSessionResponse(session, maze, moveResult);
        return Ok(response);
    }

    private static bool TryParseDirection(string direction, out Direction parsedDirection)
    {
        return Enum.TryParse(direction, ignoreCase: true, out parsedDirection);
    }

    private SessionResponse BuildSessionResponse(MazeSession session, Maze maze, MoveResult? moveResult = null)
    {
        var generatedLinks = _linkGenerator.GenerateLinks(session, maze);
        var links = generatedLinks.ToDictionary(kvp => kvp.Key, kvp => (Link)kvp.Value);

        var response = new SessionResponse
        {
            Id = session.Id,
            MazeId = session.MazeId,
            CurrentPosition = new PositionDto(session.CurrentPosition.X, session.CurrentPosition.Y),
            State = session.State.ToString(),
            StartedAt = session.StartedAt,
            Links = links
        };

        if (moveResult.HasValue)
        {
            response.MoveResult = moveResult.Value.ToString();
        }

        if (session.State == SessionState.Completed)
        {
            response.Message = "Congratulations! You've completed the maze!";
        }

        return response;
    }
}
