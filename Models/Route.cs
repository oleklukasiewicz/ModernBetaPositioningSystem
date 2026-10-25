namespace ModernBetaPositioningSystem.Models;

public class Route
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Feature> Checkpoints { get; set; } = new();
    public bool IsRailway { get; set; }
    public List<Tag> Tags { get; set; } = new();
}
