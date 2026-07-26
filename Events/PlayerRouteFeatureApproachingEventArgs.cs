using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureApproachingEventArgs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public Models.Route Route { get; private set; }
        public Feature ApproachingFeature { get; private set; }
        public Feature HeadingTo { get; private set; }
        public bool IsInsideApproachingFeature { get; set; }
        public PlayerRouteFeatureApproachingEventArgs(PlayerPosition playerPosition, Models.Route route, Feature approachingFeature, Feature headingTo, bool isInsideApproachingFeature)
        {
            PlayerPosition = playerPosition;
            Route = route;
            ApproachingFeature = approachingFeature;
            HeadingTo = headingTo;
            IsInsideApproachingFeature = isInsideApproachingFeature;
        }
    }
}
