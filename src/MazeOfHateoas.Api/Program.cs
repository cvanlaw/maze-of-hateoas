using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
