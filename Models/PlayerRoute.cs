namespace ModernBetaPositioningSystem.Models
{
    public class PlayerRoute
    {
        public string Username { get; set; }
        public Route Route { get; set; }
        public PlayerPosition Position { get; set; }
        public Feature CurrentFeature { get; set; }
        public Feature HeadingTo { get; set; }

        public DateTime? OffRouteSince { get; set; }
        public bool IsJustAdded { get; set; } = false;
        public Guid? LastLeftFeatureId { get; set; }
    }
}
