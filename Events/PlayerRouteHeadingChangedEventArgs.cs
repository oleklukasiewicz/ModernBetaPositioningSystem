using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteHeadingChangedEventargs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public Feature PreviousHeading { get; private set; }

        public PlayerRouteHeadingChangedEventargs(PlayerRoute playerRoute, Feature previousHeading)
        {
            PlayerRoute = playerRoute;
            PreviousHeading = previousHeading;
        }

    }
}
