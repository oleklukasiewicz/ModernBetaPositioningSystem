namespace ModernBetaPositioningSystem.Models;

public class PlayerRoute
{
    public string Username { get; set; } = string.Empty;
    public Route Route { get; set; } = null!;
    public PlayerPosition Position { get; set; } = null!;
    public Feature CurrentFeature { get; set; } = null!;
    public Feature HeadingTo { get; set; } = null!;

    public DateTime? OffRouteSince { get; set; }
    public bool IsJustAdded { get; set; }
    public Guid? LastLeftFeatureId { get; set; }
}
