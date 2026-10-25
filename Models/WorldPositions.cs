namespace ModernBetaPositioningSystem.Models;

public class WorldPositions
{
    public string World { get; set; } = string.Empty;
    public List<Position> Players { get; set; } = new();
}
