using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureChangedEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public Feature PreviousFeature { get; private set; }
        public PlayerRouteFeatureChangedEventArgs(PlayerRoute playerRoute, Feature previousFeature)
        {
            PlayerRoute = playerRoute;
            PreviousFeature = previousFeature;
        }
    }
}
