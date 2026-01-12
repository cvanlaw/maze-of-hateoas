using Microsoft.AspNetCore.SignalR;

namespace MazeOfHateoas.Api.Hubs;

public class MetricsHub : Hub
{
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
    }

    public async Task SubscribeToMaze(Guid mazeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"maze:{mazeId}");
    }

    public async Task Unsubscribe()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");
    }

    public async Task UnsubscribeFromMaze(Guid mazeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"maze:{mazeId}");
    }
}
