using ModernBetaPositioningSystem.Models;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.ResponseModels
{
    public class FeatureResponseModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PositionResponseModel StartPosition { get; set; }
        public PositionResponseModel EndPosition { get; set; }
        public List<Tag>? Tags { get; set; } = new();
        public List<RouteSimpleResponseModel>? Transfers { get; set; }
        public FeatureResponseModel(Feature feature)
        {
            Id = feature?.Id ?? Guid.Empty;
            Name = feature?.Name ?? string.Empty;
            StartPosition = new PositionResponseModel(feature?.StartPosition);
            EndPosition = new PositionResponseModel(feature?.EndPosition);
            Tags = feature?.Tags;
            Transfers = feature?.Transfers?.Select(t => new RouteSimpleResponseModel(t)).ToList() ?? new List<RouteSimpleResponseModel>();
        }
    }
}
