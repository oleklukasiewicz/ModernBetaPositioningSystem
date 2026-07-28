namespace ModernBetaPositioningSystem.Models;

public class Feature
{
    private Position _start = null!, _end = null!;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MinX { get; private set; }
    public long MaxX { get; private set; }
    public long MinY { get; private set; }
    public long MaxY { get; private set; }
    public long MinZ { get; private set; }
    public long MaxZ { get; private set; }
    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double CenterZ { get; private set; }

    public Position StartPosition { get => _start; set { _start = value; Recalc(); } }
    public Position EndPosition { get => _end; set { _end = value; Recalc(); } }

    public List<Tag> Tags { get; set; } = new List<Tag>();
    private void Recalc()
    {
        if (_start == null || _end == null) return;
        (MinX, MaxX) = (Math.Min(_start.X, _end.X), Math.Max(_start.X, _end.X));
        (MinY, MaxY) = (Math.Min(_start.Y, _end.Y), Math.Max(_start.Y, _end.Y));
        (MinZ, MaxZ) = (Math.Min(_start.Z, _end.Z), Math.Max(_start.Z, _end.Z));
        (CenterX, CenterY, CenterZ) = ((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0, (MinZ + MaxZ) / 2.0);
    }

    public Position CenterPosition => new() { X = (long)CenterX, Y = (long)CenterY, Z = (long)CenterZ };

    public double DistanceFrom(Position p) =>
        Math.Sqrt(Math.Pow(Math.Max(0, Math.Max(MinX - p.X, p.X - MaxX)), 2) +
                  Math.Pow(Math.Max(0, Math.Max(MinY - p.Y, p.Y - MaxY)), 2) +
                  Math.Pow(Math.Max(0, Math.Max(MinZ - p.Z, p.Z - MaxZ)), 2));

    public double DistanceSquaredToCenterFrom(Position p) => Math.Pow(p.X - CenterX, 2) + Math.Pow(p.Y - CenterY, 2) + Math.Pow(p.Z - CenterZ, 2);

    public int GetDirection(PlayerPosition pos)
    {
        if (pos?.ActualPosition == null || pos.PreviousPosition == null || pos.MovementVector == null) return 0;
        var dir = pos.MovementVector;
        if (dir.X == 0 && dir.Y == 0 && dir.Z == 0) return 0;

        if (IsLocationInFeature(pos.ActualPosition))
            return DistanceSquaredToCenterFrom(pos.ActualPosition).CompareTo(DistanceSquaredToCenterFrom(pos.PreviousPosition));

        if (TryGetRayIntersection(pos.ActualPosition, dir, out double tmin, out double tmax))
            return (tmax >= 0 && tmin > 0) ? -1 : (tmax < 0 ? 1 : 0);

        return 0;
    }

    public bool IsLocationInFeature(Position p) => p.X >= MinX - 2 && p.X <= MaxX + 2 && p.Y >= MinY - 2 && p.Y <= MaxY + 2 && p.Z >= MinZ - 2 && p.Z <= MaxZ + 2;

    private bool TryGetRayIntersection(Position origin, Position dir, out double tmin, out double tmax)
    {
        (tmin, tmax) = (double.NegativeInfinity, double.PositiveInfinity);
        (long minX, long maxX, long minY, long maxY, long minZ, long maxZ) = (MinX - 2, MaxX + 2, MinY - 2, MaxY + 2, MinZ - 2, MaxZ + 2);

        if (dir.X != 0) { double t1 = (minX - origin.X) / (double)dir.X, t2 = (maxX - origin.X) / (double)dir.X; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
        else if (origin.X < minX || origin.X > maxX) return false;

        if (dir.Y != 0) { double t1 = (minY - origin.Y) / (double)dir.Y, t2 = (maxY - origin.Y) / (double)dir.Y; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
        else if (origin.Y < minY || origin.Y > maxY) return false;

        if (dir.Z != 0) { double t1 = (minZ - origin.Z) / (double)dir.Z, t2 = (maxZ - origin.Z) / (double)dir.Z; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
        else if (origin.Z < minZ || origin.Z > maxZ) return false;

        return tmax >= tmin;
    }

    public bool IsLeaving(PlayerPosition pos) => GetDirection(pos) > 0;
    public bool IsApproaching(PlayerPosition pos) => GetDirection(pos) < 0;
    public bool IsHeadingTo(PlayerPosition pos) => pos?.ActualPosition != null && pos.MovementVector != null &&
        ((CenterX - pos.ActualPosition.X) * pos.MovementVector.X + (CenterY - pos.ActualPosition.Y) * pos.MovementVector.Y + (CenterZ - pos.ActualPosition.Z) * pos.MovementVector.Z) > 0;

    public bool IsInRange(Position p, Position r) => p.X >= MinX - r.X && p.X <= MaxX + r.X && p.Y >= MinY - r.Y && p.Y <= MaxY + r.Y && p.Z >= MinZ - r.Z && p.Z <= MaxZ + r.Z;
}