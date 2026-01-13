using MazeOfHateoas.Solver.Configuration;
using MazeOfHateoas.Solver.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var section = builder.Configuration.GetSection(SolverSettings.SectionName);
        section.Bind(options);

        // Environment variable overrides
        options.ApiBaseUrl = builder.Configuration["SOLVER_API_BASE_URL"] ?? options.ApiBaseUrl;
        options.MazeWidth = int.TryParse(builder.Configuration["SOLVER_MAZE_WIDTH"], out var w) ? w : options.MazeWidth;
        options.MazeHeight = int.TryParse(builder.Configuration["SOLVER_MAZE_HEIGHT"], out var h) ? h : options.MazeHeight;
        options.DelayBetweenMazesMs = int.TryParse(builder.Configuration["SOLVER_DELAY_BETWEEN_MAZES_MS"], out var dm) ? dm : options.DelayBetweenMazesMs;
        options.DelayBetweenMovesMs = int.TryParse(builder.Configuration["SOLVER_DELAY_BETWEEN_MOVES_MS"], out var dmv) ? dmv : options.DelayBetweenMovesMs;
        options.StatsIntervalMazes = int.TryParse(builder.Configuration["SOLVER_STATS_INTERVAL_MAZES"], out var si) ? si : options.StatsIntervalMazes;
        options.Algorithm = builder.Configuration["SOLVER_ALGORITHM"] ?? options.Algorithm;
    });

    builder.Services.AddHttpClient<IMazeApiClient, MazeApiClient>((sp, client) =>
    {
        var config = builder.Configuration;
        client.BaseAddress = new Uri(config["SOLVER_API_BASE_URL"] ?? "http://localhost:8080");
    });

    builder.Services.AddSingleton<ISolver>(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<SolverSettings>>().Value;
        var apiClient = sp.GetRequiredService<IMazeApiClient>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        var algorithm = settings.Algorithm.ToLowerInvariant();
        Log.Information("Using solver algorithm: {Algorithm}", algorithm);

        return algorithm switch
        {
            "bfs" => new BreadthFirstSolver(
                apiClient,
                sp.GetRequiredService<IOptions<SolverSettings>>(),
                loggerFactory.CreateLogger<BreadthFirstSolver>()),
            "random" => new RandomWalkSolver(
                apiClient,
                sp.GetRequiredService<IOptions<SolverSettings>>(),
                loggerFactory.CreateLogger<RandomWalkSolver>()),
            _ => new DepthFirstSolver(
                apiClient,
                sp.GetRequiredService<IOptions<SolverSettings>>(),
                loggerFactory.CreateLogger<DepthFirstSolver>())
        };
    });
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
