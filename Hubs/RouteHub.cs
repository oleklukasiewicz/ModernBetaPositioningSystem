using Microsoft.AspNetCore.SignalR;

namespace ModernBetaPositioningSystem.Hubs;

public class RouteHub : Hub
{
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

    private static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        return username.Trim();
    }
}