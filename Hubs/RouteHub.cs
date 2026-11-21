using Microsoft.AspNetCore.SignalR;
using ModernBetaPositioningSystem.Services;

namespace ModernBetaPositioningSystem.Hubs;

public class RouteHub : Hub
{
    private readonly PositionService _positionService;

    public RouteHub(PositionService positionService)
    {
        _positionService = positionService;
    }

    public bool Hello(string username)
    {
        return _positionService.IsTracked(username);
    }

    public Task TrackUser(string username)
    {
        return SetGroupAsync(username, addToGroup: true);
    }

    public Task UntrackUser(string username)
    {
        return SetGroupAsync(username, addToGroup: false);
    }

    private async Task SetGroupAsync(string username, bool addToGroup)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (string.IsNullOrEmpty(normalizedUsername))
            return;

        if (addToGroup)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUsername);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalizedUsername);
    }

    private static string? NormalizeUsername(string? username) =>
        PositionService.NormalizeUsername(username);
}