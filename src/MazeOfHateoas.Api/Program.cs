using System.Reflection;
using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Maze of HATEOAS API",
        Description = "A RESTful API demonstrating HATEOAS principles through interactive maze navigation. " +
                      "Generate mazes and navigate through them by following hypermedia links that indicate available moves.",
        Contact = new OpenApiContact
        {
            Name = "API Support"
        },
        License = new OpenApiLicense
        {
            Name = "MIT"
        }
    });

    // Include XML comments from the API assembly
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configure MazeSettings from environment variables
builder.Services.Configure<MazeSettings>(options =>
{
    var defaultWidth = Environment.GetEnvironmentVariable("MAZE_DEFAULT_WIDTH");
    var defaultHeight = Environment.GetEnvironmentVariable("MAZE_DEFAULT_HEIGHT");
    var maxWidth = Environment.GetEnvironmentVariable("MAZE_MAX_WIDTH");
    var maxHeight = Environment.GetEnvironmentVariable("MAZE_MAX_HEIGHT");

    if (int.TryParse(defaultWidth, out var width))
        options.DefaultWidth = width;

    if (int.TryParse(defaultHeight, out var height))
        options.DefaultHeight = height;

    if (int.TryParse(maxWidth, out var mWidth))
        options.MaxWidth = mWidth;

    if (int.TryParse(maxHeight, out var mHeight))
        options.MaxHeight = mHeight;
});

// Register application services
builder.Services.AddSingleton<IMazeGenerator, MazeGenerator>();
builder.Services.AddSingleton<IMazeRepository, InMemoryMazeRepository>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<ISessionLinkGenerator, SessionLinkGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Global exception handler - returns Problem Details for unhandled exceptions
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Maze of HATEOAS API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
