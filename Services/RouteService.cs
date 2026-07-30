using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.Services;

public class RouteService
{
    private readonly double _maxDist;
    private readonly int _graceSec;
    private readonly Position _rangeThreshold;

    private Dictionary<Guid, Feature> _features = new();
    private readonly Dictionary<Guid, Route> _routes = new();
    private readonly Dictionary<(string, Guid), PlayerRoute> _playerRoutes = new();

    public RouteService(Position? rangeThreshold = null, int gracePeriodSeconds = 8, double maxAllowedDistanceFromRoute = 32.0)
    {
        _rangeThreshold = rangeThreshold ?? new Position(32, 0, 32);
        _graceSec = gracePeriodSeconds;
        _maxDist = maxAllowedDistanceFromRoute;
    }

    public event EventHandler<PlayerRouteFeatureApproachingEventArgs>? OnApproach;
    public event EventHandler<PlayerRouteFeatureLeavingEventArgs>? OnLeave;
    public event EventHandler<PlayerRouteFeatureChangedEventArgs>? OnFeatureChanged;
    public event EventHandler<PlayerRouteHeadingChangedEventargs>? OnHeadingChanged;
    public event EventHandler<PlayerRouteAddedEventArgs>? OnPlayerRouteAdded;
    public event EventHandler<PlayerRouteDisposedEventArgs>? OnPlayerRouteDisposed;
    public event EventHandler<PlayerRouteUpdateEventArgs>? OnPlayerRouteUpdate;

    public void AddRoute(Route route)
    {
        _routes[route.Id] = route;
        foreach (var cp in route.Checkpoints)
            _features[cp.Id] = cp;
    }
    public Feature GetClosestFeature(Position position)
    {
        if (_features.Count == 0) return null;

        Feature closestFeature = null;
        double minDistance = double.MaxValue;

        foreach (var feature in _features.Values)
        {
            double distance = feature.DistanceFrom(position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestFeature = feature;
            }
        }
        return closestFeature;

    }
    public PlayerRoute? AddPlayerRoute(PlayerRoute pr)
    {
        if (!_playerRoutes.TryAdd((pr.Username, pr.Route.Id), pr)) return null;
        OnPlayerRouteAdded?.Invoke(this, new PlayerRouteAddedEventArgs(pr));
        return pr;
    }

    public void RemovePlayerRoute(PlayerRoute pr)
    {
        if (_playerRoutes.Remove((pr.Username, pr.Route.Id)))
            OnPlayerRouteDisposed?.Invoke(this, new PlayerRouteDisposedEventArgs(pr));
    }

    public void DetectCheckpoint(PlayerPosition pos, Feature? closest, PlayerRoute pr)
    {
        var prCheckpoints = pr.Route.Checkpoints.Where(f => f.IsInvisible != true).ToList();
        if (closest == null || !closest.IsInRange(pos.ActualPosition, _rangeThreshold)) return;
        if (closest.IsInvisible == true)
            return;
        int idx = prCheckpoints.FindIndex(f => f.Id == closest.Id);
        if (idx == -1 || (pr.Route.IsRailway && !pos.IsInMinecart)) return;

        var (oldCurrent, oldHeading) = (pr.CurrentFeature, pr.HeadingTo);

        bool inLoc = closest.IsLocationInFeature(pos.ActualPosition);
        bool appr = closest.IsApproaching(pos), leav = closest.IsLeaving(pos);

        OnPlayerRouteUpdate?.Invoke(this, new PlayerRouteUpdateEventArgs(pr, inLoc, appr, leav));

        if (!pr.IsJustAdded)
        {
            if (inLoc) { pr.CurrentFeature = closest; pr.LastLeftFeatureId = null; }
            else if (leav && pr.CurrentFeature?.Id == closest.Id) pr.CurrentFeature = null;
        }

        var pts = prCheckpoints;
        var next = idx + 1 < pts.Count ? pts[idx + 1] : null;
        var prev = idx - 1 >= 0 ? pts[idx - 1] : null;

        if (next?.IsHeadingTo(pos) == true) pr.HeadingTo = next;
        else if (prev?.IsHeadingTo(pos) == true) pr.HeadingTo = prev;

        if (pr.IsJustAdded)
        {
            if (pr.HeadingTo != null && oldHeading?.Id != pr.HeadingTo?.Id)
                OnHeadingChanged?.Invoke(this, new PlayerRouteHeadingChangedEventargs(pr, oldHeading));
            pr.IsJustAdded = false;
            return;
        }

        if (appr && oldCurrent?.Id != pr.CurrentFeature?.Id)
            OnApproach?.Invoke(this, new PlayerRouteFeatureApproachingEventArgs(pr, closest, inLoc));
        if (leav && pr.CurrentFeature == null && pr.LastLeftFeatureId != closest.Id)
        {
            pr.LastLeftFeatureId = closest.Id;
            OnLeave?.Invoke(this, new PlayerRouteFeatureLeavingEventArgs(pr, closest));
        }
        if (oldCurrent?.Id != pr.CurrentFeature?.Id)
            OnFeatureChanged?.Invoke(this, new PlayerRouteFeatureChangedEventArgs(pr, oldCurrent));
        if (oldHeading?.Id != pr.HeadingTo?.Id)
            OnHeadingChanged?.Invoke(this, new PlayerRouteHeadingChangedEventargs(pr, oldHeading));
    }

    public void DetectActiveRoutes(PlayerPosition pos, Feature closest)
    {
        var now = DateTime.Now;
        double? closestDist = closest?.DistanceFrom(pos.ActualPosition);

        List<PlayerRoute> values = new List<PlayerRoute>();
        lock (_playerRoutes)
        {
            values = _playerRoutes.Values.ToList();
        }
        for (var i = 0; i < values.Count; i++)
        {
            var pr = values[i];
            if (pr == null)
                continue;
            if (pr.Username != pos.Username) continue;
            double dist = pr.CurrentFeature?.DistanceFrom(pos.ActualPosition) ?? pr.HeadingTo?.DistanceFrom(pos.ActualPosition) ?? closestDist ?? double.MaxValue;

            if (dist > _maxDist || (pr.Route.IsRailway && !pos.IsInMinecart && pr.CurrentFeature == null))
            {
                pr.OffRouteSince ??= now;

                int effectiveGrace = (pr.Route.IsRailway && pos.IsInMinecart) ? _graceSec * 4 : _graceSec;

                if ((now - pr.OffRouteSince.Value).TotalSeconds >= effectiveGrace) RemovePlayerRoute(pr);
            }
            else pr.OffRouteSince = null;
        }

        if (closest == null || (closestDist ?? double.MaxValue) > _maxDist) return;

        foreach (var r in _routes.Values)
        {
            if (r.IsRailway && !pos.IsInMinecart) continue;
            if (!r.Checkpoints.Any(c => c.Id == closest.Id)) continue;

            if (!_playerRoutes.TryGetValue((pos.Username, r.Id), out var pr))
            {
                pr = new PlayerRoute { Username = pos.Username, Route = r, IsJustAdded = true };
                AddPlayerRoute(pr);
            }
            pr.Position = pos;
            DetectCheckpoint(pos, closest, pr);
        }
    }
}