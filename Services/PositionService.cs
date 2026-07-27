using Microsoft.AspNetCore.Http.HttpResults;
using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Services
{
    public class PositionService
    {
        private readonly string _endpointURL;
        private readonly string _apiKey;
        private HttpClient _httpClient;

        public List<PlayerPosition> _trackedPlayers = new List<PlayerPosition>();

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
            //new + update
            foreach (var player in worldPositions.Players)
            {
                var existingPlayer = _trackedPlayers.FirstOrDefault(p => p.Username == player.Username);
                if (existingPlayer != null)
                {
                    var previousPosition = existingPlayer;
                    var updatedPlayer = existingPlayer.UpdatePosition(player);
                    OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(updatedPlayer, previousPosition));
                }
                else
                {
                    var newPlayer = new PlayerPosition(player.Username, player);
                    _trackedPlayers.Add(newPlayer);
                    OnPlayerPositionAdded?.Invoke(this, new PlayerPositionAddedEventArgs(newPlayer));
                    OnPlayerPositionUpdated?.Invoke(this, new PlayerPositionUpdatedEventArgs(newPlayer, null));
                }
            }
            //untracking
            for (int i = 0; i < _trackedPlayers.Count; i++)
            {
                var trackedPlayer = _trackedPlayers[i];
                if (!worldPositions.Players.Any(p => p.Username == trackedPlayer.Username))
                {
                    trackedPlayer.UnTrack();
                    _trackedPlayers.Remove(trackedPlayer);
                    OnPlayerPositionDisposed?.Invoke(this, new PlayerPositionDisposedEventArgs(trackedPlayer));
                }
            }

            OnPositionsFetched?.Invoke(this, new PlayerPositionsFetchedEventArgs(_trackedPlayers));

            return _trackedPlayers;
        }
        private async Task<WorldPositions> _GetPositions()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _endpointURL);
            request.Headers.Add("X-API-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var worldPositions = await response.Content.ReadFromJsonAsync<WorldPositions>();

            return worldPositions;
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
