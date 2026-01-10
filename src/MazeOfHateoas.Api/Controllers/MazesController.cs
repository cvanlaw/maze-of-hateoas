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

    [HttpGet("{id}")]
    public async Task<ActionResult<MazeResponse>> GetMaze(Guid id)
    {
        var maze = await _mazeRepository.GetByIdAsync(id);

        if (maze == null)
            return NotFound();

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
