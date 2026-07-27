using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteUpdateEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public bool IsApproaching { get; private set; }
        public bool IsLeaving { get; private set; }
        public bool IsInside { get; private set; }

        public PlayerRouteUpdateEventArgs(PlayerRoute playerRoute, bool isApproaching, bool isLeaving, bool isInside)
        {
            PlayerRoute = playerRoute;
            IsApproaching = isApproaching;
            IsLeaving = isLeaving;
            IsInside = isInside;
        }
    }
}
