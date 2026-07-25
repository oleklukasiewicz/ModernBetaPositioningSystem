using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Services
{
    public class FeatureService
    {
        private Position _closestDistanceThreshold = new Position(20, 0, 20); // Example threshold value, adjust as needed
        private Dictionary<Guid, Feature> _features;
        public FeatureService(Position closestDistanceThreshold = null)
        {
            if (closestDistanceThreshold == null)
            {
                closestDistanceThreshold = new Position(20, 0, 20);
            }
            _closestDistanceThreshold = closestDistanceThreshold;
            _features = new Dictionary<Guid, Feature>();
        }
        public void AddFeature(Feature feature)
        {
            if (!_features.ContainsKey(feature.Id))
            {
                _features.Add(feature.Id, feature);
            }
        }
        public void RemoveFeature(Guid featureId)
        {
            if (_features.ContainsKey(featureId))
            {
                _features.Remove(featureId);
            }
        }
        public Feature GetClosest(Position position)
        {
            if (_features.Count == 0) return null;

            Feature closestFeature = null;
            double minDistance = double.MaxValue;

            foreach (var feature in _features.Values)
            {
                double distance = GetDistance(feature, position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestFeature = feature;
                }
            }
            if (IsInRange(position, closestFeature))
            {
                return closestFeature;
            }

            return null;
        }

        private double GetDistance(Feature feature, Position position)
        {
            long minX = Math.Min(feature.StartPosition.X, feature.EndPosition.X);
            long maxX = Math.Max(feature.StartPosition.X, feature.EndPosition.X);
            long minY = Math.Min(feature.StartPosition.Y, feature.EndPosition.Y);
            long maxY = Math.Max(feature.StartPosition.Y, feature.EndPosition.Y);
            long minZ = Math.Min(feature.StartPosition.Z, feature.EndPosition.Z);
            long maxZ = Math.Max(feature.StartPosition.Z, feature.EndPosition.Z);

            double dx = Math.Max(0, Math.Max(minX - position.X, position.X - maxX));
            double dy = Math.Max(0, Math.Max(minY - position.Y, position.Y - maxY));
            double dz = Math.Max(0, Math.Max(minZ - position.Z, position.Z - maxZ));

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public bool IsPlayerApprochingFeature(PlayerPosition player, Feature feature) => GetMovementDirection(player, feature) < 0;
        public bool IsPlayerLeavingFeature(PlayerPosition player, Feature feature) => GetMovementDirection(player, feature) > 0;

        private int GetMovementDirection(PlayerPosition player, Feature feature)
        {
            if (player?.ActualPosition == null || player?.PreviousPosition == null || feature == null) return 0;
            var dir = player.MovementVector;
            if (dir.X == 0 && dir.Y == 0 && dir.Z == 0) return 0;

            if (IsPlayerInFeature(player, feature))
            {
                return GetDistanceSquaredToCenter(player.ActualPosition, feature)
                    .CompareTo(GetDistanceSquaredToCenter(player.PreviousPosition, feature));
            }

            if (TryGetRayIntersection(player.ActualPosition, dir, feature, out double tmin, out double tmax))
            {
                if (tmax >= 0 && tmin > 0) return -1;
                if (tmax < 0) return 1;               
            }
            return 0;
        }

        public bool IsPlayerInFeature(PlayerPosition player, Feature feature)
        {
            long tolerance = 2;
            GetFeatureBounds(feature, tolerance, tolerance, tolerance, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);

            return player.ActualPosition.X >= minX && player.ActualPosition.X <= maxX &&
                   player.ActualPosition.Y >= minY && player.ActualPosition.Y <= maxY &&
                   player.ActualPosition.Z >= minZ && player.ActualPosition.Z <= maxZ;
        }

        private double GetDistanceSquaredToCenter(Position position, Feature feature)
        {
            double centerX = (feature.StartPosition.X + feature.EndPosition.X) / 2.0;
            double centerY = (feature.StartPosition.Y + feature.EndPosition.Y) / 2.0;
            double centerZ = (feature.StartPosition.Z + feature.EndPosition.Z) / 2.0;

            double dx = position.X - centerX;
            double dy = position.Y - centerY;
            double dz = position.Z - centerZ;

            return dx * dx + dy * dy + dz * dz;
        }

        private void GetFeatureBounds(Feature feature, long tolX, long tolY, long tolZ, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ)
        {
            minX = Math.Min(feature.StartPosition.X, feature.EndPosition.X) - tolX;
            maxX = Math.Max(feature.StartPosition.X, feature.EndPosition.X) + tolX;
            minY = Math.Min(feature.StartPosition.Y, feature.EndPosition.Y) - tolY;
            maxY = Math.Max(feature.StartPosition.Y, feature.EndPosition.Y) + tolY;
            minZ = Math.Min(feature.StartPosition.Z, feature.EndPosition.Z) - tolZ;
            maxZ = Math.Max(feature.StartPosition.Z, feature.EndPosition.Z) + tolZ;
        }

        private bool TryGetRayIntersection(Position origin, Position dir, Feature feature, out double tmin, out double tmax)
        {
            tmin = double.NegativeInfinity;
            tmax = double.PositiveInfinity;

            long tolerance = 2;
            GetFeatureBounds(feature, tolerance, tolerance, tolerance, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);

            if (dir.X != 0)
            {
                double tx1 = (minX - origin.X) / (double)dir.X;
                double tx2 = (maxX - origin.X) / (double)dir.X;
                tmin = Math.Max(tmin, Math.Min(tx1, tx2));
                tmax = Math.Min(tmax, Math.Max(tx1, tx2));
            }
            else if (origin.X < minX || origin.X > maxX) return false;

            if (dir.Y != 0)
            {
                double ty1 = (minY - origin.Y) / (double)dir.Y;
                double ty2 = (maxY - origin.Y) / (double)dir.Y;
                tmin = Math.Max(tmin, Math.Min(ty1, ty2));
                tmax = Math.Min(tmax, Math.Max(ty1, ty2));
            }
            else if (origin.Y < minY || origin.Y > maxY) return false;

            if (dir.Z != 0)
            {
                double tz1 = (minZ - origin.Z) / (double)dir.Z;
                double tz2 = (maxZ - origin.Z) / (double)dir.Z;
                tmin = Math.Max(tmin, Math.Min(tz1, tz2));
                tmax = Math.Min(tmax, Math.Max(tz1, tz2));
            }
            else if (origin.Z < minZ || origin.Z > maxZ) return false;

            return tmax >= tmin;
        }

        public bool IsInRange(Position position, Feature feature)
        {
            GetFeatureBounds(feature, _closestDistanceThreshold.X, _closestDistanceThreshold.Y, _closestDistanceThreshold.Z, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);

            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minY && position.Y <= maxY &&
                   position.Z >= minZ && position.Z <= maxZ;
        }
    }
}
