using System.Diagnostics;

namespace ModernBetaPositioningSystem.Services
{
    public class TrackingService
    {
        private readonly PositionService _positionService;
        private readonly FeatureService _featureService;

        public TrackingService()
        {
            _positionService = new PositionService("https://api.modernbeta.org/api/v1/worlds/world/positions", "16_O4PQQ0LBsEf9JmVs_tnjeWsYDAhzQCrf55MKTYfU");
            _featureService = new FeatureService();
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "A4",
                StartPosition = new Position { X = -376, Y = 47, Z = -2632 },
                EndPosition = new Position { X = -368, Y = 45, Z = -2603 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C3",
                StartPosition = new Position { X = -595, Y = 49, Z = -2695 },
                EndPosition = new Position { X = -589, Y = 47, Z = -2675 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C4",
                StartPosition = new Position { X = -532, Y = 55, Z = -2618 },
                EndPosition = new Position { X = -495, Y = 53, Z = -2610 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C5",
                StartPosition = new Position { X = -333, Y = 45, Z = -2598 },
                EndPosition = new Position { X = -355, Y = 43, Z = -2590 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C6",
                StartPosition = new Position { X = -252, Y = 41, Z = -2516 },
                EndPosition = new Position { X = -283, Y = 39, Z = -2507 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C7",
                StartPosition = new Position { X = -216, Y = 56, Z = -2587 },
                EndPosition = new Position { X = -186, Y = 54, Z = -2597 }
            });
            _featureService.AddFeature(new Models.Feature
            {
                Id = Guid.NewGuid(),
                Name = "C8",
                StartPosition = new Position { X = -104, Y = 58, Z = -2639 },
                EndPosition = new Position { X = -81, Y = 56, Z = -2583 }
            });
        }
        public async Task StartTrackingLoop(int intervalInSeconds, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _positionService.Track();
                    foreach (var player in result)
                    {

                        if (player.Username == "olek128")
                        {
                            var guid = _featureService.GetClosest(player.ActualPosition);
                            if (guid != null)
                            {
                                var isApproaching = _featureService.IsPlayerApprochingFeature(player, guid);
                                var isInside = _featureService.IsPlayerInFeature(player, guid);
                                var isleaving = _featureService.IsPlayerLeavingFeature(player, guid);
                                Debug.WriteLine($"Player: {player.Username}, Closest Feature: {guid.Name}, IsApproaching: {isApproaching}, IsInside: {isInside}, IsLeaving: {isleaving}");
                            }
                            else
                                Debug.WriteLine($"Player: {player.Username}, No feature nearby.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in tracking loop: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds), cancellationToken);
            }
        }
    }
}
