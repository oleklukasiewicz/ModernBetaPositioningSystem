namespace ModernBetaPositioningSystem.Models
{
    public class Feature
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Position StartPosition { get; set; }
        public Position EndPosition { get; set; }

        public double DistanceFrom(Position position)
        {
            long minX = Math.Min(StartPosition.X, EndPosition.X);
            long maxX = Math.Max(StartPosition.X, EndPosition.X);
            long minY = Math.Min(StartPosition.Y, EndPosition.Y);
            long maxY = Math.Max(StartPosition.Y, EndPosition.Y);
            long minZ = Math.Min(StartPosition.Z, EndPosition.Z);
            long maxZ = Math.Max(StartPosition.Z, EndPosition.Z);

            double dx = Math.Max(0, Math.Max(minX - position.X, position.X - maxX));
            double dy = Math.Max(0, Math.Max(minY - position.Y, position.Y - maxY));
            double dz = Math.Max(0, Math.Max(minZ - position.Z, position.Z - maxZ));

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public Position CenterPosition => new Position
        {
            X = (StartPosition.X + EndPosition.X) / 2,
            Y = (StartPosition.Y + EndPosition.Y) / 2,
            Z = (StartPosition.Z + EndPosition.Z) / 2
        };
        public double DistanceSquaredToCenterFrom(Position position)
        {
            double dx = position.X - CenterPosition.X;
            double dy = position.Y - CenterPosition.Y;
            double dz = position.Z - CenterPosition.Z;
            return dx * dx + dy * dy + dz * dz;
        }
        public void Bounds(long tolX, long tolY, long tolZ, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ)
        {
            minX = Math.Min(StartPosition.X, EndPosition.X) - tolX;
            maxX = Math.Max(StartPosition.X, EndPosition.X) + tolX;
            minY = Math.Min(StartPosition.Y, EndPosition.Y) - tolY;
            maxY = Math.Max(StartPosition.Y, EndPosition.Y) + tolY;
            minZ = Math.Min(StartPosition.Z, EndPosition.Z) - tolZ;
            maxZ = Math.Max(StartPosition.Z, EndPosition.Z) + tolZ;
        }
        public int GetDirection(PlayerPosition playerPosition)
        {
            if (playerPosition?.ActualPosition == null || playerPosition?.PreviousPosition == null || this == null) return 0;
            var dir = playerPosition.MovementVector;
            if (dir.X == 0 && dir.Y == 0 && dir.Z == 0) return 0;

            if (IsLocationInFeature(playerPosition.ActualPosition))
            {
                return DistanceSquaredToCenterFrom(playerPosition.ActualPosition)
                    .CompareTo(DistanceSquaredToCenterFrom(playerPosition.PreviousPosition));
            }

            if (TryGetRayIntersection(playerPosition.ActualPosition, dir, out double tmin, out double tmax))
            {
                if (tmax >= 0 && tmin > 0) return -1;
                if (tmax < 0) return 1;
            }
            return 0;
        }
        public bool IsLocationInFeature(Position position)
        {
            long tolerance = 2;
            Bounds(tolerance, tolerance, tolerance, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);
            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minY && position.Y <= maxY &&
                   position.Z >= minZ && position.Z <= maxZ;
        }
        private bool TryGetRayIntersection(Position origin, Position dir, out double tmin, out double tmax)
        {
            tmin = double.NegativeInfinity;
            tmax = double.PositiveInfinity;

            long tolerance = 2;
            Bounds(tolerance, tolerance, tolerance, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);

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
        public bool IsLeaving(PlayerPosition playerPosition) => GetDirection(playerPosition) > 0;
        public bool IsApproaching(PlayerPosition playerPosition) => GetDirection(playerPosition) < 0;
        public bool IsHeadingTo(PlayerPosition playerPosition)
        {
            if (playerPosition == null || playerPosition.MovementVector == null)
                return false;

            double dirX = CenterPosition.X - playerPosition.ActualPosition.X;
            double dirY = CenterPosition.Y - playerPosition.ActualPosition.Y;
            double dirZ = CenterPosition.Z - playerPosition.ActualPosition.Z;

            double dotProduct = playerPosition.MovementVector.X * dirX + playerPosition.MovementVector.Y * dirY + playerPosition.MovementVector.Z * dirZ;

            return dotProduct > 0;
        }
        public bool IsInRange(Position position, Position range)
        {
            Bounds(range.X, range.Y, range.Z, out long minX, out long maxX, out long minY, out long maxY, out long minZ, out long maxZ);

            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minY && position.Y <= maxY &&
                   position.Z >= minZ && position.Z <= maxZ;
        }
    }
}
