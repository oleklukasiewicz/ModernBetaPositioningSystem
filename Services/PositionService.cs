using Microsoft.AspNetCore.Http.HttpResults;
using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using System.Collections.Concurrent;

namespace ModernBetaPositioningSystem.Services
{
    public class PositionService
    {
        private readonly string _endpointURL;
        private readonly string _apiKey;
        private HttpClient _httpClient;

        private readonly ConcurrentDictionary<string, PlayerPosition> _trackedPlayers = new();

        public event EventHandler<PlayerPositionsFetchedEventArgs>? OnPositionsFetched;
        public event EventHandler<PlayerPositionUpdatedEventArgs>? OnPlayerPositionUpdated;
        public event EventHandler<PlayerPositionAddedEventArgs>? OnPlayerPositionAdded;
        public event EventHandler<PlayerPositionDisposedEventArgs>? OnPlayerPositionDisposed;
        public PositionService(string endpointURL, string apiKey, HttpClient httpClient = null)
        {
            _endpointURL = endpointURL;
            _apiKey = apiKey;
            _httpClient = httpClient ?? new HttpClient();
        }
        public async Task<List<PlayerPosition>> Track()
        {
            var worldPositions = await _GetPositions();
            if (worldPositions == null)
                return _trackedPlayers.Values.ToList();

            if (worldPositions?.Players == null) return _trackedPlayers.Values.ToList();
            var currentServerUsernames = new HashSet<string>(
                worldPositions.Players.Select(p => p.Username),
                StringComparer.OrdinalIgnoreCase
            );

            await Parallel.ForEachAsync(worldPositions.Players, (player, ct) =>
            {
                _trackedPlayers.AddOrUpdate(
                    player.Username,
                    key =>
                    {
                        var newPlayer = new PlayerPosition(key, player);
                        OnPlayerPositionAdded?.Invoke(this, new PlayerPositionAddedEventArgs(newPlayer));
                        OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(newPlayer, null));
                        return newPlayer;
                    },
                    (key, existingPlayer) =>
                    {
                        var previousPosition = existingPlayer;
                        var updatedPlayer = existingPlayer.UpdatePosition(player);
                        OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(updatedPlayer, previousPosition));
                        return updatedPlayer;
                    }
                );

                return ValueTask.CompletedTask;
            });

            foreach (var (username, trackedPlayer) in _trackedPlayers)
            {
                if (!currentServerUsernames.Contains(username))
                {
                    if (_trackedPlayers.TryRemove(username, out var removedPlayer))
                    {
                        removedPlayer.UnTrack();
                        OnPlayerPositionDisposed?.Invoke(this, new PlayerPositionDisposedEventArgs(removedPlayer));
                    }
                }
            }

            var resultList = _trackedPlayers.Values.ToList();
            OnPositionsFetched?.Invoke(this, new PlayerPositionsFetchedEventArgs(resultList));

            return resultList;
        }
        private async Task<WorldPositions> _GetPositions()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _endpointURL);
                request.Headers.Add("X-API-Key", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var worldPositions = await response.Content.ReadFromJsonAsync<WorldPositions>();

                return worldPositions;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching positions: {ex.Message}");
                throw;
            }
            return null;
        }
        public async Task StartTrackingLoop(CancellationToken cancellationToken, int intervalInSeconds = 1)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var results = await Track();
                await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds), cancellationToken);
            }
        }
    }
}
