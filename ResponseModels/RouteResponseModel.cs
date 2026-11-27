using ModernBetaPositioningSystem.Models;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.ResponseModels
{
    public class RouteResponseModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<FeatureResponseModel> Checkpoints { get; set; } = new();
        public bool IsRailway { get; set; }
        public List<Tag> Tags { get; set; } = new();
        public RouteResponseModel(Route route)
        {
            Id = route?.Id ?? Guid.Empty;
            Name = route?.Name ?? string.Empty;
            Checkpoints = route?.Checkpoints?.Select(c => new FeatureResponseModel(c)).ToList() ?? new List<FeatureResponseModel>();
            IsRailway = route?.IsRailway ?? false;
            Tags = route?.Tags;
        }
    }
}
