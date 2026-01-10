using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Models;
using MazeOfHateoas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MazeOfHateoas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MazesController : ControllerBase
{
    private readonly IMazeGenerator _mazeGenerator;
    private readonly IMazeRepository _mazeRepository;
    private readonly MazeSettings _settings;

    public MazesController(
        IMazeGenerator mazeGenerator,
        IMazeRepository mazeRepository,
        IOptions<MazeSettings> settings)
    {
        _mazeGenerator = mazeGenerator;
        _mazeRepository = mazeRepository;
        _settings = settings.Value;
    }

    [HttpPost]
    public async Task<ActionResult<MazeResponse>> CreateMaze([FromBody] CreateMazeRequest? request)
    {
        var width = request?.Width ?? _settings.DefaultWidth;
        var height = request?.Height ?? _settings.DefaultHeight;

        // Validate dimensions are positive
        if (width <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = "Width must be a positive integer",
                Instance = "/api/mazes"
            });
        }

        if (height <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = "Height must be a positive integer",
                Instance = "/api/mazes"
            });
        }

        // Validate dimensions don't exceed max
        if (width > _settings.MaxWidth)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = $"Width cannot exceed {_settings.MaxWidth}",
                Instance = "/api/mazes"
            });
        }

        if (height > _settings.MaxHeight)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = $"Height cannot exceed {_settings.MaxHeight}",
                Instance = "/api/mazes"
            });
        }

        var maze = _mazeGenerator.Generate(width, height);
        await _mazeRepository.SaveAsync(maze);

        var response = new MazeResponse
        {
            Id = maze.Id,
            Width = maze.Width,
            Height = maze.Height,
            Start = new PositionDto(maze.Start.X, maze.Start.Y),
            End = new PositionDto(maze.End.X, maze.End.Y),
            CreatedAt = maze.CreatedAt,
            Links = new Dictionary<string, Link>
            {
                ["self"] = new Link($"/api/mazes/{maze.Id}", "self", "GET"),
                ["start"] = new Link($"/api/mazes/{maze.Id}/sessions", "start", "POST")
            }
        };

        return CreatedAtAction(nameof(GetMaze), new { id = maze.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<MazeListResponse>> GetAllMazes()
    {
        var mazes = await _mazeRepository.GetAllAsync();

        var response = new MazeListResponse
        {
            Mazes = mazes.Select(maze => new MazeSummaryResponse
            {
                Id = maze.Id,
                Width = maze.Width,
                Height = maze.Height,
                CreatedAt = maze.CreatedAt,
                Links = new Dictionary<string, Link>
                {
                    ["self"] = new Link($"/api/mazes/{maze.Id}", "self", "GET"),
                    ["start"] = new Link($"/api/mazes/{maze.Id}/sessions", "start", "POST")
                }
            }).ToList(),
            Links = new Dictionary<string, Link>
            {
                ["self"] = new Link("/api/mazes", "self", "GET"),
                ["create"] = new Link("/api/mazes", "create", "POST")
            }
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MazeResponse>> GetMaze(Guid id)
    {
        var maze = await _mazeRepository.GetByIdAsync(id);

        if (maze == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"Maze with ID '{id}' was not found",
                Instance = $"/api/mazes/{id}"
            });
        }

        var response = new MazeResponse
        {
            Id = maze.Id,
            Width = maze.Width,
            Height = maze.Height,
            Start = new PositionDto(maze.Start.X, maze.Start.Y),
            End = new PositionDto(maze.End.X, maze.End.Y),
            CreatedAt = maze.CreatedAt,
            Links = new Dictionary<string, Link>
            {
                ["self"] = new Link($"/api/mazes/{maze.Id}", "self", "GET"),
                ["start"] = new Link($"/api/mazes/{maze.Id}/sessions", "start", "POST")
            }
        };

        return Ok(response);
    }
}
