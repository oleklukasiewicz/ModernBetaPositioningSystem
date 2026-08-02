using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Services;

public class PositionService
{
    private readonly string _endpointUrl;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, PlayerPosition> _trackedPlayers = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<PlayerPositionsFetchedEventArgs>? OnPositionsFetched;
    public event EventHandler<PlayerPositionUpdatedEventArgs>? OnPlayerPositionUpdated;
    public event EventHandler<PlayerPositionAddedEventArgs>? OnPlayerPositionAdded;
    public event EventHandler<PlayerPositionDisposedEventArgs>? OnPlayerPositionDisposed;

    public PositionService(string endpointUrl, string apiKey, HttpClient? httpClient = null)
    {
        _endpointUrl = endpointUrl;
        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<List<PlayerPosition>> Track()
    {
        var worldPositions = await FetchPositionsAsync();
        if (worldPositions?.Players == null)
            return _trackedPlayers.Values.ToList();

        var activeUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var player in worldPositions.Players)
        {
            activeUsernames.Add(player.Username);

            _trackedPlayers.AddOrUpdate(
                player.Username,
                key =>
                {
                    var newPlayer = new PlayerPosition(key, player);
                    OnPlayerPositionAdded?.Invoke(this, new PlayerPositionAddedEventArgs(newPlayer));
                    OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(newPlayer, null));
                    return newPlayer;
                },
                (_, existing) =>
                {
                    var prev = existing;
                    var updated = existing.UpdatePosition(player);
                    OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(updated, prev));
                    return updated;
                });
        }

        foreach (var key in _trackedPlayers.Keys)
        {
            if (!activeUsernames.Contains(key) && _trackedPlayers.TryRemove(key, out var removed))
            {
                removed.UnTrack();
                OnPlayerPositionDisposed?.Invoke(this, new PlayerPositionDisposedEventArgs(removed));
            }
        }

        var resultList = _trackedPlayers.Values.ToList();
        OnPositionsFetched?.Invoke(this, new PlayerPositionsFetchedEventArgs(resultList));
        return resultList;
    }

    private async Task<WorldPositions?> FetchPositionsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WorldPositions>(_endpointUrl);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching positions: {ex.Message}");
            return null;
        }
    }

    public async Task StartTrackingLoop(CancellationToken ct, int intervalInSeconds = 1)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalInSeconds));
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await Track();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in tracking loop: {ex.Message}");
            }
        }
    }
}