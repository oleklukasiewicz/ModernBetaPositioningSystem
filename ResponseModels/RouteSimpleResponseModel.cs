using ModernBetaPositioningSystem.Models;
using Route = ModernBetaPositioningSystem.Models.Route;

namespace ModernBetaPositioningSystem.ResponseModels
{
    public class RouteSimpleResponseModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRailway { get; set; }
        public List<Tag> Tags { get; set; } = new();
        public RouteSimpleResponseModel(Route route)
        {
            Id = route?.Id ?? Guid.Empty;
            Name = route?.Name ?? string.Empty;
            IsRailway = route?.IsRailway ?? false;
            Tags = route?.Tags;
        }
    }
}
