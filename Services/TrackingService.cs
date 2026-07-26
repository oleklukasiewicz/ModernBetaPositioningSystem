using System.Diagnostics;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.Services
{
    public class TrackingService
    {
        private readonly PositionService _positionService;
        private readonly FeatureService _featureService;
        private readonly RouteService _routeService;
        public TrackingService(string positionServiceUrl, string positionServiceApiKey)
        {
            _positionService = new PositionService(positionServiceUrl, positionServiceApiKey);
            _featureService = new FeatureService();
            _routeService = new RouteService();

            RegisterRouteEvents();
        }
        private void RegisterRouteEvents()
        {
            _routeService.OnApproach += (sender, e) =>
            {
                if (e.IsInsideApproachingFeature)
                    Debug.WriteLine($"{e.PlayerPosition.Username}> [THIS IS] {e.ApproachingFeature.Name}");
            };

            _routeService.OnLeave += (sender, e) =>
            {
                if (e.HeadingTo != null)
                    Debug.WriteLine($"{e.PlayerPosition.Username}> [NEXT STATION IS] {e.HeadingTo?.Name}");
            };

            _routeService.OnPlayerRouteAdded += (sender, e) =>
            {
                //if (e.Feature == null && e.PreviousFeature != null)
                //    Debug.WriteLine($"[NEXT STATION IS] {e.HeadingTo.Name}");
                Debug.WriteLine($"[PLAYER ROUTE ADDED] {e.PlayerRoute.Username} added to route: {e.PlayerRoute.Route.Name}");
            };

            _routeService.OnPlayerRouteDisposed += (sender, e) =>
            {
                Debug.WriteLine($"[PLAYER ROUTE DISPOSED] {e.PlayerRoute.Username} removed from route: {e.PlayerRoute.Route.Name}");
            };
            _routeService.OnPlayerRouteUpdate += (sender, e) =>
            {
                // Debug.WriteLine($"[PLAYER ROUTE UPDATE] {e.PlayerRoute.Username} Feature:{e.Feature.Name}, IsInside:{e.IsInside}, IsApproaching:{e.IsApproaching}, IsLeaving:{e.IsLeaving}");
            };
        }
        public void RegisterRoutes(List<Route> config)
        {
            foreach (var route in config)
            {
                _routeService.AddRoute(route);
                foreach (var feature in route.Checkpoints)
                {
                    _featureService.AddFeature(feature);
                }
            }
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
                        var closestFeature = _featureService.GetClosest(player.ActualPosition);

                        _routeService.DetectActiveRoutes(player, closestFeature);
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
