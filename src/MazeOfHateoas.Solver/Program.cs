using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

try
{
    Log.Information("Starting Maze Solver");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    builder.Services.Configure<SolverSettings>(options =>
    {
        options.ApiBaseUrl = builder.Configuration["SOLVER_API_BASE_URL"] ?? "http://localhost:8080";
        options.MazeWidth = int.Parse(builder.Configuration["SOLVER_MAZE_WIDTH"] ?? "10");
        options.MazeHeight = int.Parse(builder.Configuration["SOLVER_MAZE_HEIGHT"] ?? "10");
        options.DelayBetweenMazesMs = int.Parse(builder.Configuration["SOLVER_DELAY_BETWEEN_MAZES_MS"] ?? "2000");
        options.DelayBetweenMovesMs = int.Parse(builder.Configuration["SOLVER_DELAY_BETWEEN_MOVES_MS"] ?? "0");
        options.StatsIntervalMazes = int.Parse(builder.Configuration["SOLVER_STATS_INTERVAL_MAZES"] ?? "10");
    });

    builder.Services.AddHttpClient<IMazeApiClient, MazeApiClient>((sp, client) =>
    {
        var config = builder.Configuration;
        client.BaseAddress = new Uri(config["SOLVER_API_BASE_URL"] ?? "http://localhost:8080");
    });

    builder.Services.AddSingleton<ISolver, HateoasSolver>();
    builder.Services.AddHostedService<SolverHostedService>();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
