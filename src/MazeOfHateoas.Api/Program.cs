using MazeOfHateoas.Api.Configuration;
using MazeOfHateoas.Api.Services;
using MazeOfHateoas.Application.Interfaces;
using MazeOfHateoas.Application.Services;
using MazeOfHateoas.Infrastructure;

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

    if (int.TryParse(defaultWidth, out var width))
        options.DefaultWidth = width;

    if (int.TryParse(defaultHeight, out var height))
        options.DefaultHeight = height;
});

// Register application services
builder.Services.AddSingleton<IMazeGenerator, MazeGenerator>();
builder.Services.AddSingleton<IMazeRepository, InMemoryMazeRepository>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<ISessionLinkGenerator, SessionLinkGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
