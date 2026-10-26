namespace ModernBetaPositioningSystem.Models;

public class Position
{
    public static Position Zero => new();

    public string Username { get; set; } = string.Empty;
    public long X { get; set; }
    public long Y { get; set; }
    public long Z { get; set; }
    public DateTime? Time { get; set; }

    public Position()
    {
    }

    public Position(long x, long y, long z)
        : this(string.Empty, x, y, z)
    {
    }

    public Position(string username, long x, long y, long z)
    {
        Username = username;
        X = x;
        Y = y;
        Z = z;
    }
}