using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MazeOfHateoas.Api.Controllers;

[ApiController]
[Route("api/mazes/{mazeId}/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IMazeRepository _mazeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionLinkGenerator _linkGenerator;

    public SessionsController(
        IMazeRepository mazeRepository,
        ISessionRepository sessionRepository,
        ISessionLinkGenerator linkGenerator)
    {
        _mazeRepository = mazeRepository;
        _sessionRepository = sessionRepository;
        _linkGenerator = linkGenerator;
    }

    [HttpPost]
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

        var response = BuildSessionResponse(session, maze);

        return CreatedAtAction(
            nameof(GetSession),
            new { mazeId = session.MazeId, sessionId = session.Id },
            response);
    }

    [HttpGet("{sessionId}")]
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

    private SessionResponse BuildSessionResponse(MazeSession session, Maze maze)
    {
        var generatedLinks = _linkGenerator.GenerateLinks(session, maze);
        var links = generatedLinks.ToDictionary(kvp => kvp.Key, kvp => (Link)kvp.Value);

        return new SessionResponse
        {
            Id = session.Id,
            MazeId = session.MazeId,
            CurrentPosition = new PositionDto(session.CurrentPosition.X, session.CurrentPosition.Y),
            State = session.State.ToString(),
            StartedAt = session.StartedAt,
            Links = links
        };
    }
}
