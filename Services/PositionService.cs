using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Services
{
    public class PositionService
    {
        private readonly string _endpointURL;
        private readonly string _apiKey;
        private HttpClient _httpClient;

        public List<PlayerPosition> _trackedPlayers = new List<PlayerPosition>();
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
                    existingPlayer.UpdatePosition(player);
                }
                else
                {
                    _trackedPlayers.Add(new PlayerPosition(player.Username, player));
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
                }
            }
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

    }
}
