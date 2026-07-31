using ModernBetaPositioningSystem.Events;
using ModernBetaPositioningSystem.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using static System.Collections.Specialized.BitVector32;
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
        if (closest == null || closest.IsInvisible == true || !closest.IsInRange(pos.ActualPosition, _rangeThreshold))
            return;

        var prCheckpoints = pr.Route.Checkpoints;
        int idx = prCheckpoints.FindIndex(f => f.Id == closest.Id);
        if (idx == -1 || (pr.Route.IsRailway && !pos.IsInMinecart))
            return;

        var (oldCurrent, oldHeading) = (pr.CurrentFeature, pr.HeadingTo);
        bool inLoc = closest.IsLocationInFeature(pos.ActualPosition);
        bool appr = closest.IsApproaching(pos);
        bool leav = closest.IsLeaving(pos);

        OnPlayerRouteUpdate?.Invoke(this, new PlayerRouteUpdateEventArgs(pr, inLoc, appr, leav));

        if (!pr.IsJustAdded)
        {
            if (inLoc) { pr.CurrentFeature = closest; pr.LastLeftFeatureId = null; }
            else if (leav && pr.CurrentFeature?.Id == closest.Id) pr.CurrentFeature = null;
        }

        var next = idx + 1 < prCheckpoints.Count ? prCheckpoints[idx + 1] : null;
        var prev = idx - 1 >= 0 ? prCheckpoints[idx - 1] : null;

        if (next?.IsHeadingTo(pos) == true) pr.HeadingTo = next;
        else if (prev?.IsHeadingTo(pos) == true) pr.HeadingTo = prev;

        if (pr.IsJustAdded)
        {
            if (pr.HeadingTo != null && oldHeading?.Id != pr.HeadingTo.Id)
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

    public void DetectActiveRoutes(PlayerPosition pos, Feature? closest)
    {
        var now = DateTime.Now;
        double? closestDist = closest?.DistanceFrom(pos.ActualPosition);

        foreach (var pr in _playerRoutes.Values.ToList())
        {
            if (pr.Username != pos.Username) continue;

            double dist = GetDistanceToRoute(pos, pr.Route);
            bool isOffRoute = dist > _maxDist || (pr.Route.IsRailway && !pos.IsInMinecart && pr.CurrentFeature == null);

            if (isOffRoute)
            {
                pr.OffRouteSince ??= now;

                int effectiveGrace = (pr.Route.IsRailway && pos.IsInMinecart) ? _graceSec * 4 : _graceSec;
                double offRouteDuration = (now - pr.OffRouteSince.Value).TotalSeconds;

                if (offRouteDuration >= effectiveGrace)
                {
                    RemovePlayerRoute(pr);
                }
            }
            else
            {
                pr.OffRouteSince = null;
            }
        }

        if (closest == null || (closestDist ?? double.MaxValue) > _maxDist)
            return;

        foreach (var r in _routes.Values)
        {
            if (r.IsRailway && !pos.IsInMinecart) continue;
            if (!r.Checkpoints.Exists(c => c.Id == closest.Id)) continue;

            var pr = _playerRoutes.GetOrAdd((pos.Username, r.Id), _ =>
            {
                var newPr = new PlayerRoute { Username = pos.Username, Route = r, IsJustAdded = true };
                OnPlayerRouteAdded?.Invoke(this, new PlayerRouteAddedEventArgs(newPr));
                return newPr;
            });

            pr.Position = pos;
            DetectCheckpoint(pos, closest, pr);
        }
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