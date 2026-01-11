using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Helpers;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Api.Controllers;

/// <summary>
/// Controller for managing mazes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MazesController : ControllerBase
{
    private readonly IMazeGenerator _mazeGenerator;
    private readonly IMazeRepository _mazeRepository;
    private readonly MazeSettings _settings;
    private readonly ILogger<MazesController> _logger;
    private readonly IMazeLinkGenerator _linkGenerator;

    public MazesController(
        IMazeGenerator mazeGenerator,
        IMazeRepository mazeRepository,
        IOptions<MazeSettings> settings,
        ILogger<MazesController> logger,
        IMazeLinkGenerator linkGenerator)
    {
        _mazeGenerator = mazeGenerator;
        _mazeRepository = mazeRepository;
        _settings = settings.Value;
        _logger = logger;
        _linkGenerator = linkGenerator;
    }

    /// <summary>
    /// Creates a new maze with the specified dimensions.
    /// </summary>
    /// <remarks>
    /// If dimensions are not provided, default values from configuration will be used.
    /// The maze is generated using a recursive backtracking algorithm ensuring a solvable path
    /// from start (0,0) to end (width-1, height-1).
    /// </remarks>
    /// <param name="request">Optional request body with width and height dimensions.</param>
    /// <returns>The created maze with HATEOAS links.</returns>
    /// <response code="201">Returns the newly created maze.</response>
    /// <response code="400">If the dimensions are invalid (negative or exceeding maximum).</response>
    [HttpPost]
    [ProducesResponseType(typeof(MazeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MazeResponse>> CreateMaze([FromBody] CreateMazeRequest? request)
    {
        var width = request?.Width ?? _settings.DefaultWidth;
        var height = request?.Height ?? _settings.DefaultHeight;

        // Validate dimensions are positive
        if (width <= 0)
        {
            return BadRequest(ApiProblemDetails.BadRequest(
                "Width must be a positive integer", "/api/mazes"));
        }

        if (height <= 0)
        {
            return BadRequest(ApiProblemDetails.BadRequest(
                "Height must be a positive integer", "/api/mazes"));
        }

        // Validate dimensions don't exceed max
        if (width > _settings.MaxWidth)
        {
            return BadRequest(ApiProblemDetails.BadRequest(
                $"Width cannot exceed {_settings.MaxWidth}", "/api/mazes"));
        }

        if (height > _settings.MaxHeight)
        {
            return BadRequest(ApiProblemDetails.BadRequest(
                $"Height cannot exceed {_settings.MaxHeight}", "/api/mazes"));
        }

        var maze = _mazeGenerator.Generate(width, height);
        await _mazeRepository.SaveAsync(maze);

        _logger.LogInformation("Maze created: {MazeId} ({Width}x{Height})",
            maze.Id, width, height);

        var response = BuildMazeResponse(maze);

        return CreatedAtAction(nameof(GetMaze), new { id = maze.Id }, response);
    }

    /// <summary>
    /// Retrieves all available mazes.
    /// </summary>
    /// <remarks>
    /// Returns a list of all mazes with summary information and HATEOAS links
    /// for navigation and starting new sessions.
    /// </remarks>
    /// <returns>A list of all mazes with HATEOAS links.</returns>
    /// <response code="200">Returns the list of mazes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MazeListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MazeListResponse>> GetAllMazes()
    {
        var mazes = await _mazeRepository.GetAllAsync();

        var response = new MazeListResponse
        {
            Mazes = mazes.Select(BuildMazeSummaryResponse).ToList(),
            Links = ConvertLinks(_linkGenerator.GenerateListLinks())
        };

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific maze by its ID.
    /// </summary>
    /// <remarks>
    /// Returns detailed maze information including start and end positions,
    /// along with HATEOAS links to start a new navigation session.
    /// </remarks>
    /// <param name="id">The unique identifier of the maze.</param>
    /// <returns>The maze details with HATEOAS links.</returns>
    /// <response code="200">Returns the requested maze.</response>
    /// <response code="404">If the maze with the specified ID was not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MazeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MazeResponse>> GetMaze(Guid id)
    {
        var maze = await _mazeRepository.GetByIdAsync(id);

        if (maze == null)
        {
            _logger.LogWarning("Maze not found: {MazeId}", id);
            return NotFound(ApiProblemDetails.NotFound(
                $"Maze with ID '{id}' was not found", $"/api/mazes/{id}"));
        }

        var response = BuildMazeResponse(maze);

        return Ok(response);
    }

    private MazeResponse BuildMazeResponse(Maze maze)
    {
        return new MazeResponse
        {
            Id = maze.Id,
            Width = maze.Width,
            Height = maze.Height,
            Start = new PositionDto(maze.Start.X, maze.Start.Y),
            End = new PositionDto(maze.End.X, maze.End.Y),
            CreatedAt = maze.CreatedAt,
            Links = ConvertLinks(_linkGenerator.GenerateMazeLinks(maze.Id))
        };
    }

    private MazeSummaryResponse BuildMazeSummaryResponse(Maze maze)
    {
        return new MazeSummaryResponse
        {
            Id = maze.Id,
            Width = maze.Width,
            Height = maze.Height,
            CreatedAt = maze.CreatedAt,
            Links = ConvertLinks(_linkGenerator.GenerateMazeLinks(maze.Id))
        };
    }

    private static Dictionary<string, Link> ConvertLinks(Dictionary<string, object> links)
    {
        return links.ToDictionary(
            kvp => kvp.Key,
            kvp => (Link)kvp.Value);
    }
}
