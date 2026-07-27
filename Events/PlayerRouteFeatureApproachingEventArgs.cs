using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureApproachingEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public Feature ApproachingFeature { get; private set; }
        public bool IsInsideApproachingFeature { get; set; }
        public PlayerRouteFeatureApproachingEventArgs(PlayerRoute playerRoute, Feature approachingFeature, bool isInsideApproachingFeature)
        {
            PlayerRoute = playerRoute;
            ApproachingFeature = approachingFeature;
            IsInsideApproachingFeature = isInsideApproachingFeature;
        }
    }
}
