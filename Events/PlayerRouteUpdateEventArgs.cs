using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteUpdateEventArgs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public PlayerRoute PlayerRoute { get; private set; }

        public Feature Feature { get; private set; }
        public bool IsApproaching { get; private set; }
        public bool IsLeaving { get; private set; }
        public bool IsInside { get; private set; }

        public PlayerRouteUpdateEventArgs(PlayerPosition playerPosition, PlayerRoute playerRoute, Feature feature, bool isApproaching, bool isLeaving, bool isInside)
        {
            PlayerPosition = playerPosition;
            PlayerRoute = playerRoute;
            Feature = feature;
            IsApproaching = isApproaching;
            IsLeaving = isLeaving;
            IsInside = isInside;
        }
    }
}
