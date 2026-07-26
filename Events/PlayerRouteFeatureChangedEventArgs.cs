using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteFeatureChangedEventArgs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public Models.Route Route { get; private set; }
        public Feature Feature { get; private set; }
        public Feature PreviousFeature { get; private set; }
        public Feature HeadingTo { get; private set; }
        public PlayerRouteFeatureChangedEventArgs(PlayerPosition playerPosition, Models.Route route, Feature feature, Feature previousFeature, Feature headingTo)
        {
            PlayerPosition = playerPosition;
            Route = route;
            Feature = feature;
            PreviousFeature = previousFeature;
            HeadingTo = headingTo;
        }
    }
}
