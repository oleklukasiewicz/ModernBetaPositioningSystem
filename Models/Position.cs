public class Position
{
    public string Username { get; set; }
    public long X { get; set; }
    public long Y { get; set; }
    public long Z { get; set; }
    public DateTime? Time { get; set; }
    public Position()
    {

    }
    public Position(long x, long y, long z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}