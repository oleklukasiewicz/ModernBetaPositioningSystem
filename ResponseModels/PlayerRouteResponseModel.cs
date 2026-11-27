using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.ResponseModels
{
    public class PlayerRouteResponseModel
    {
        public string Username { get; set; } = string.Empty;
        public RouteResponseModel Route { get; set; } = null!;
        public PositionResponseModel Position { get; set; } = null!;
        public FeatureResponseModel CurrentFeature { get; set; } = null!;
        public FeatureResponseModel HeadingTo { get; set; } = null!;

        public DateTime? OffRouteSince { get; set; }
        public bool IsJustAdded { get; set; }
        public Guid? LastLeftFeatureId { get; set; }
        public PlayerRouteResponseModel(PlayerRoute playerRoute)
        {
            Username = playerRoute?.Username;
            Route = new RouteResponseModel(playerRoute?.Route);
            Position = new PositionResponseModel(playerRoute?.Position);
            CurrentFeature = new FeatureResponseModel(playerRoute?.CurrentFeature);
            HeadingTo = new FeatureResponseModel(playerRoute?.HeadingTo);
            OffRouteSince = playerRoute?.OffRouteSince;
            IsJustAdded = playerRoute?.IsJustAdded ?? false;
            LastLeftFeatureId = playerRoute?.LastLeftFeatureId;
        }
    }
}
