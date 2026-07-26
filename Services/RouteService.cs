using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.Services;

public class RouteService
{
    private readonly double _maxAllowedDistanceFromRoute;
    private readonly int _gracePeriodSeconds;
    private readonly Position _rangeThreshold;

    private readonly Dictionary<Guid, Route> _routes = new();
    private readonly Dictionary<(string Username, Guid RouteId), PlayerRoute> _playerRoutes = new();

    public RouteService(Position? rangeThreshold = null, int gracePeriodSeconds = 8, double maxAllowedDistanceFromRoute = 32.0)
    {
        _rangeThreshold = rangeThreshold ?? new Position(24, 0, 24);
        _gracePeriodSeconds = gracePeriodSeconds;
        _maxAllowedDistanceFromRoute = maxAllowedDistanceFromRoute;
    }

    public event EventHandler<PlayerRouteFeatureApproachingEventArgs>? OnApproach;
    public event EventHandler<PlayerRouteFeatureLeavingEventArgs>? OnLeave;
    public event EventHandler<PlayerRouteFeatureChangedEventArgs>? OnFeatureChanged;
    public event EventHandler<PlayerRouteHeadingChangedEventargs>? OnHeadingChanged;
    public event EventHandler<PlayerRouteAddedEventArgs>? OnPlayerRouteAdded;
    public event EventHandler<PlayerRouteDisposedEventArgs>? OnPlayerRouteDisposed;
    public event EventHandler<PlayerRouteUpdateEventArgs>? OnPlayerRouteUpdate;

    public void AddRoute(Route route) => _routes[route.Id] = route;

    public PlayerRoute? AddPlayerRoute(PlayerRoute playerRoute)
    {
        if (!_playerRoutes.TryAdd((playerRoute.Username, playerRoute.Route.Id), playerRoute))
            return null;

        OnPlayerRouteAdded?.Invoke(this, new PlayerRouteAddedEventArgs(playerRoute));
        return playerRoute;
    }

    public void RemovePlayerRoute(PlayerRoute playerRoute)
    {
        if (_playerRoutes.Remove((playerRoute.Username, playerRoute.Route.Id)))
            OnPlayerRouteDisposed?.Invoke(this, new PlayerRouteDisposedEventArgs(playerRoute));
    }

    public bool IsOffByDistance(PlayerPosition playerPosition, Feature? closestFeature) =>
        closestFeature == null || closestFeature.DistanceFrom(playerPosition.ActualPosition) > _maxAllowedDistanceFromRoute;

    public (double Distance, Feature? Feature) GetClosestDistanceToFeatureInRoute(PlayerPosition playerPosition, Route route)
    {
        var minDistance = double.MaxValue;
        Feature? closestFeature = null;

        foreach (var feature in route.Checkpoints)
        {
            var distance = feature.DistanceFrom(playerPosition.ActualPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFeature = feature;
            }
        }

        return (minDistance, closestFeature);
    }

    public void DetectCheckpoint(PlayerPosition playerPosition, Feature? closestFeature, PlayerRoute pr)
    {
        if (closestFeature == null || !closestFeature.IsInRange(playerPosition.ActualPosition, _rangeThreshold)) return;

        int featureIndex = pr.Route.Checkpoints.FindIndex(f => f.Id == closestFeature.Id);
        if (featureIndex == -1 || (pr.Route.IsRailway && !playerPosition.IsInMinecart)) return;

        var oldCurrentFeature = pr.CurrentFeature;
        var oldHeadingTo = pr.HeadingTo;

        bool isInside = closestFeature.IsLocationInFeature(playerPosition.ActualPosition);
        bool isApproaching = closestFeature.IsApproaching(playerPosition);
        bool isLeaving = closestFeature.IsLeaving(playerPosition);

        OnPlayerRouteUpdate?.Invoke(this, new PlayerRouteUpdateEventArgs(playerPosition, pr, closestFeature, isInside, isApproaching, isLeaving));

        if (!pr.IsJustAdded)
        {
            if (isInside)
                pr.CurrentFeature = closestFeature;
            else if (isLeaving && pr.CurrentFeature?.Id == closestFeature.Id)
                pr.CurrentFeature = null;
        }

        var checkpoints = pr.Route.Checkpoints;
        var nextFeature = featureIndex + 1 < checkpoints.Count ? checkpoints[featureIndex + 1] : null;
        var previousFeature = featureIndex - 1 >= 0 ? checkpoints[featureIndex - 1] : null;

        if (nextFeature?.IsHeadingTo(playerPosition) == true)
            pr.HeadingTo = nextFeature;
        else if (previousFeature?.IsHeadingTo(playerPosition) == true)
            pr.HeadingTo = previousFeature;

        if (pr.IsJustAdded)
        {
            if (pr.HeadingTo != null && oldHeadingTo?.Id != pr.HeadingTo?.Id)
                OnHeadingChanged?.Invoke(this, new PlayerRouteHeadingChangedEventargs(playerPosition, pr.Route, pr.HeadingTo, oldHeadingTo));

            pr.IsJustAdded = false;
            return;
        }

        if (isApproaching && oldCurrentFeature?.Id != pr.CurrentFeature?.Id)
            OnApproach?.Invoke(this, new PlayerRouteFeatureApproachingEventArgs(playerPosition, pr.Route, closestFeature, pr.HeadingTo, isInside));

        if (isLeaving && oldCurrentFeature?.Id == closestFeature.Id && pr.CurrentFeature == null)
            OnLeave?.Invoke(this, new PlayerRouteFeatureLeavingEventArgs(playerPosition, pr.Route, closestFeature, pr.HeadingTo));

        if (oldCurrentFeature?.Id != pr.CurrentFeature?.Id)
            OnFeatureChanged?.Invoke(this, new PlayerRouteFeatureChangedEventArgs(playerPosition, pr.Route, pr.CurrentFeature, oldCurrentFeature, pr.HeadingTo));

        if (oldHeadingTo?.Id != pr.HeadingTo?.Id)
            OnHeadingChanged?.Invoke(this, new PlayerRouteHeadingChangedEventargs(playerPosition, pr.Route, pr.HeadingTo, oldHeadingTo));
    }

    public void DetectActiveRoutes(PlayerPosition playerPosition, Feature closestFeature)
    {
        var now = DateTime.Now;

        foreach (var playerRoute in _playerRoutes.Values.Where(pr => pr.Username == playerPosition.Username).ToList())
        {
            var (distance, _) = GetClosestDistanceToFeatureInRoute(playerPosition, playerRoute.Route);
            bool isOffRoute = distance > _maxAllowedDistanceFromRoute ||
                              (playerRoute.Route.IsRailway && !playerPosition.IsInMinecart && playerRoute.CurrentFeature == null);

            if (isOffRoute)
            {
                playerRoute.OffRouteSince ??= now;
                if ((now - playerRoute.OffRouteSince.Value).TotalSeconds >= _gracePeriodSeconds)
                    RemovePlayerRoute(playerRoute);
            }
            else
            {
                playerRoute.OffRouteSince = null;
            }
        }

        if (IsOffByDistance(playerPosition, closestFeature)) return;

        foreach (var route in _routes.Values.Where(r => r.Checkpoints.Any(c => c.Id == closestFeature.Id)))
        {
            if (route.IsRailway && !playerPosition.IsInMinecart) continue;

            if (!_playerRoutes.TryGetValue((playerPosition.Username, route.Id), out var playerRoute))
            {
                playerRoute = new PlayerRoute { Username = playerPosition.Username, Route = route, IsJustAdded = true };
                AddPlayerRoute(playerRoute);
            }

            DetectCheckpoint(playerPosition, closestFeature, playerRoute);
        }
    }
}