using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureLeavingEventArgs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public Models.Route Route { get; private set; }
        public Feature LeavingFeature { get; private set; }
        public Feature HeadingTo { get; private set; }
        public PlayerRouteFeatureLeavingEventArgs(PlayerPosition playerPosition, Models.Route route, Feature leavingFeature, Feature headingTo)
        {
            PlayerPosition = playerPosition;
            Route = route;
            LeavingFeature = leavingFeature;
            HeadingTo = headingTo;
        }
    }
}
