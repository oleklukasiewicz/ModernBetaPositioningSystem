using Microsoft.AspNetCore.SignalR;

namespace ModernBetaPositioningSystem.Hubs;

public class RouteHub : Hub
{
    // Metoda wywoływana z frontendu, przypisuje połączenie do grupy gracza
    public async Task TrackUser(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, username.Trim());
        }
    }

    // Opcjonalnie: opuszczenie śledzenia
    public async Task UntrackUser(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, username.Trim());
        }
    }
}