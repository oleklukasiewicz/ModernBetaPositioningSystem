namespace ModernBetaPositioningSystem.Models;

public class Tag
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Values { get; set; } = new();
}
