using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using System.Collections.Concurrent;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.Services;

public class RouteService
{
    private readonly double _maxDist;
    private readonly int _graceSec;
    private readonly Position _rangeThreshold;

    private readonly ConcurrentDictionary<Guid, Feature> _features = new();
    private readonly ConcurrentDictionary<Guid, Route> _routes = new();
    private readonly ConcurrentDictionary<(string Username, Guid RouteId), PlayerRoute> _playerRoutes = new();

    public event EventHandler<PlayerRouteFeatureApproachingEventArgs>? OnApproach;
    public event EventHandler<PlayerRouteFeatureLeavingEventArgs>? OnLeave;
    public event EventHandler<PlayerRouteFeatureChangedEventArgs>? OnFeatureChanged;
    public event EventHandler<PlayerRouteHeadingChangedEventargs>? OnHeadingChanged;
    public event EventHandler<PlayerRouteAddedEventArgs>? OnPlayerRouteAdded;
    public event EventHandler<PlayerRouteDisposedEventArgs>? OnPlayerRouteDisposed;
    public event EventHandler<PlayerRouteUpdateEventArgs>? OnPlayerRouteUpdate;

    public RouteService(Position? rangeThreshold = null, int gracePeriodSeconds = 4, double maxAllowedDistanceFromRoute = 32.0)
    {
        _rangeThreshold = rangeThreshold ?? new Position(24, 0, 24);
        _graceSec = gracePeriodSeconds;
        _maxDist = maxAllowedDistanceFromRoute;
    }

    public void AddRoute(Route route)
    {
        _routes[route.Id] = route;
        foreach (var cp in route.Checkpoints)
            _features[cp.Id] = cp;
    }
    public void AddRoute(List<Route> routes)
    {
        foreach (var route in routes)
            AddRoute(route);
    }

    public Feature? GetClosestFeature(Position position)
    {
        Feature? closest = null;
        double minDist = double.MaxValue;

        foreach (var feature in _features.Values)
        {
            double dist = feature.DistanceFrom(position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = feature;
            }
        }
        return closest;
    }

    public PlayerRoute? AddPlayerRoute(PlayerRoute pr)
    {
        if (!_playerRoutes.TryAdd((pr.Username, pr.Route.Id), pr)) return null;
        OnPlayerRouteAdded?.Invoke(this, new PlayerRouteAddedEventArgs(pr));
        return pr;
    }

    public void RemovePlayerRoute(PlayerRoute pr)
    {
        if (_playerRoutes.TryRemove((pr.Username, pr.Route.Id), out _))
            OnPlayerRouteDisposed?.Invoke(this, new PlayerRouteDisposedEventArgs(pr));
    }

    public void DetectCheckpoint(PlayerPosition pos, Feature? closest, PlayerRoute pr)
    {
        if (!CanProcessCheckpoint(pos, closest))
            return;

        var checkpoints = pr.Route.Checkpoints;
        int index = checkpoints.FindIndex(f => f.Id == closest!.Id);

        if (index == -1 || (pr.Route.IsRailway && !pos.IsInMinecart))
            return;

        var previousCurrent = pr.CurrentFeature;
        var previousHeading = pr.HeadingTo;
        bool isInside = closest.IsLocationInFeature(pos.ActualPosition);
        bool isApproaching = closest.IsApproaching(pos);
        bool isLeaving = closest.IsLeaving(pos);

        OnPlayerRouteUpdate?.Invoke(this, new PlayerRouteUpdateEventArgs(pr, isApproaching, isLeaving, isInside));
        UpdateCurrentFeature(pr, closest, isInside, isLeaving);
        UpdateHeading(pr, checkpoints, index, pos);

        if (pr.IsJustAdded)
        {
            NotifyHeadingChange(pr, previousHeading);
            pr.IsJustAdded = false;
            return;
        }

        if (isApproaching && previousCurrent?.Id != pr.CurrentFeature?.Id)
            OnApproach?.Invoke(this, new PlayerRouteFeatureApproachingEventArgs(pr, closest, isInside));

        if (isLeaving && pr.CurrentFeature == null && pr.LastLeftFeatureId != closest.Id)
        {
            pr.LastLeftFeatureId = closest.Id;
            OnLeave?.Invoke(this, new PlayerRouteFeatureLeavingEventArgs(pr, closest));
        }

        if (previousCurrent?.Id != pr.CurrentFeature?.Id)
            OnFeatureChanged?.Invoke(this, new PlayerRouteFeatureChangedEventArgs(pr, previousCurrent));

        NotifyHeadingChange(pr, previousHeading);
    }

    private bool CanProcessCheckpoint(PlayerPosition pos, Feature? closest) =>
        closest != null &&
        closest.IsInvisible != true &&
        closest.IsInRange(pos.ActualPosition, _rangeThreshold);

    private void UpdateCurrentFeature(PlayerRoute pr, Feature closest, bool isInside, bool isLeaving)
    {
        if (pr.IsJustAdded)
            return;

        if (isInside)
        {
            pr.CurrentFeature = closest;
            pr.LastLeftFeatureId = null;
            return;
        }

        if (isLeaving && pr.CurrentFeature?.Id == closest.Id)
            pr.CurrentFeature = null;
    }

    private void UpdateHeading(PlayerRoute pr, List<Feature> checkpoints, int index, PlayerPosition pos)
    {
        var next = index + 1 < checkpoints.Count ? checkpoints[index + 1] : null;
        var previous = index - 1 >= 0 ? checkpoints[index - 1] : null;

        if (next?.IsHeadingTo(pos) == true)
        {
            pr.HeadingTo = next;
            return;
        }

        if (previous?.IsHeadingTo(pos) == true)
            pr.HeadingTo = previous;
    }

    private void NotifyHeadingChange(PlayerRoute pr, Feature? previousHeading)
    {
        if (previousHeading?.Id != pr.HeadingTo?.Id)
            OnHeadingChanged?.Invoke(this, new PlayerRouteHeadingChangedEventargs(pr, previousHeading));
    }

    public void DetectActiveRoutes(PlayerPosition pos, Feature? closest)
    {
        RemoveOffRoutePlayers(pos);

        if (closest == null || closest.DistanceFrom(pos.ActualPosition) > _maxDist)
            return;

        foreach (var route in _routes.Values)
        {
            if (route.IsRailway && !pos.IsInMinecart)
                continue;

            if (!route.Checkpoints.Exists(checkpoint => checkpoint.Id == closest.Id))
                continue;

            var playerRoute = _playerRoutes.GetOrAdd((pos.Username, route.Id), _ => CreatePlayerRoute(pos, route));
            playerRoute.Position = pos;
            DetectCheckpoint(pos, closest, playerRoute);
        }
    }

    private void RemoveOffRoutePlayers(PlayerPosition pos)
    {
        var now = DateTime.Now;

        foreach (var playerRoute in _playerRoutes.Values.ToList())
        {
            if (playerRoute.Username != pos.Username)
                continue;

            if (IsOnRoute(pos, playerRoute))
            {
                playerRoute.OffRouteSince = null;
                continue;
            }

            playerRoute.OffRouteSince ??= now;
            if (HasExceededGracePeriod(pos, playerRoute, now))
                RemovePlayerRoute(playerRoute);
        }
    }

    private bool IsOnRoute(PlayerPosition pos, PlayerRoute playerRoute)
    {
        var routeDistance = GetDistanceToRoute(pos, playerRoute.Route);
        return routeDistance <= _maxDist && !(playerRoute.Route.IsRailway && !pos.IsInMinecart && playerRoute.CurrentFeature == null);
    }

    private bool HasExceededGracePeriod(PlayerPosition pos, PlayerRoute playerRoute, DateTime now)
    {
        var effectiveGrace = playerRoute.Route.IsRailway && pos.IsInMinecart ? _graceSec * 4 : _graceSec;
        var elapsed = (now - playerRoute.OffRouteSince!.Value).TotalSeconds;
        return elapsed >= effectiveGrace;
    }

    private PlayerRoute CreatePlayerRoute(PlayerPosition pos, Route route)
    {
        var newPlayerRoute = new PlayerRoute { Username = pos.Username, Route = route, IsJustAdded = true };
        OnPlayerRouteAdded?.Invoke(this, new PlayerRouteAddedEventArgs(newPlayerRoute));
        return newPlayerRoute;
    }

    private double GetDistanceToRoute(PlayerPosition pos, Route route)
    {
        var features = route?.Checkpoints;
        if (features == null || features.Count == 0 || pos?.ActualPosition == null)
            return double.MaxValue;

        if (features.Count == 1)
            return features[0].DistanceFrom(pos.ActualPosition);

        double minDistance = double.MaxValue;

        for (int i = 0; i < features.Count - 1; i++)
        {
            double dist = DistanceToSegment(pos.ActualPosition, features[i], features[i + 1]);
            if (dist < minDistance) minDistance = dist;
        }

        return minDistance;
    }

    private double DistanceToSegment(Position p, Feature f1, Feature f2)
    {
        double ax = f1.CenterX, ay = f1.CenterY, az = f1.CenterZ;
        double bx = f2.CenterX, by = f2.CenterY, bz = f2.CenterZ;

        double abx = bx - ax, aby = by - ay, abz = bz - az;
        double apx = p.X - ax, apy = p.Y - ay, apz = p.Z - az;
        double abLenSq = abx * abx + aby * aby + abz * abz;

        if (abLenSq == 0)
            return f1.DistanceFrom(p);

        double t = Math.Clamp((apx * abx + apy * aby + apz * abz) / abLenSq, 0.0, 1.0);

        Position projPoint = new()
        {
            X = (long)(ax + t * abx),
            Y = (long)(ay + t * aby),
            Z = (long)(az + t * abz)
        };

        double dx = p.X - projPoint.X;
        double dy = p.Y - projPoint.Y;
        double dz = p.Z - projPoint.Z;

        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}