namespace ModernBetaPositioningSystem.Models
{
    public class Feature
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Position StartPosition { get; set; }
        public Position EndPosition { get; set; }
    }
}
