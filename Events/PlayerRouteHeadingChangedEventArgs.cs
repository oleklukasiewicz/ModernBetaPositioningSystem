using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteHeadingChangedEventargs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public Models.Route Route { get; private set; }
        public Feature HeadingTo { get; private set; }
        public Feature PreviousHeading { get; private set; }

        public PlayerRouteHeadingChangedEventargs(PlayerPosition playerPosition, Models.Route route, Feature headingTo, Feature previousHeading)
        {
            PlayerPosition = playerPosition;
            Route = route;
            HeadingTo = headingTo;
            PreviousHeading = previousHeading;
        }

    }
}
