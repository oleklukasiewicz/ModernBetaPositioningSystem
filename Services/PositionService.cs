using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using System.Collections.Concurrent;

namespace ModernBetaPositioningSystem.Services;

public class PositionService
{
    private readonly string _endpointUrl;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, PlayerPosition> _trackedPlayers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, WorldPositions>? _converter;

    public event EventHandler<PlayerPositionsFetchedEventArgs>? OnPositionsFetched;
    public event EventHandler<PlayerPositionUpdatedEventArgs>? OnPlayerPositionUpdated;
    public event EventHandler<PlayerPositionAddedEventArgs>? OnPlayerPositionAdded;
    public event EventHandler<PlayerPositionDisposedEventArgs>? OnPlayerPositionDisposed;

    public PositionService(string endpointUrl, string apiKey, HttpClient? httpClient = null, Func<string, WorldPositions>? converter = null)
    {
        _endpointUrl = endpointUrl;
        _httpClient = httpClient ?? new HttpClient();

        if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        _converter = converter ?? DeserializeWorldPositions;
    }

    private static WorldPositions DeserializeWorldPositions(string payload)
    {
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<WorldPositions>(payload, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (deserialized == null)
            throw new InvalidOperationException("Failed to deserialize WorldPositions from payload.");

        return deserialized;
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
                OnPlayerPositionDisposed?.Invoke(this, new PlayerPositionDisposedEventArgs(removed));
        }

        var resultList = _trackedPlayers.Values.ToList();
        OnPositionsFetched?.Invoke(this, new PlayerPositionsFetchedEventArgs(resultList));
        return resultList;
    }

    private async Task<WorldPositions?> FetchPositionsAsync()
    {
        try
        {
            var stringResponse = await _httpClient.GetStringAsync(_endpointUrl);
            return _converter(stringResponse);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching positions: {ex.Message}");
            return null;
        }
    }

    public async Task StartTrackingLoop(CancellationToken ct, int milliseconds = 1010)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(milliseconds));
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await Track();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in tracking loop: {ex.Message}");
            }
        }
    }
}