using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteAddedEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public PlayerRouteAddedEventArgs(PlayerRoute playerRoute)
        {
            PlayerRoute = playerRoute;
        }
    }
}
