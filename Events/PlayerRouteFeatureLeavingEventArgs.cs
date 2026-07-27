using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureLeavingEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public Feature LeavingFeature { get; private set; }
        public PlayerRouteFeatureLeavingEventArgs(PlayerRoute playerRoute, Feature leavingFeature)
        {
            PlayerRoute = playerRoute;
            LeavingFeature = leavingFeature;
        }
    }
}
