using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerRouteDisposedEventArgs : EventArgs
    {
        public PlayerRoute PlayerRoute { get; private set; }
        public PlayerRouteDisposedEventArgs(PlayerRoute playerRoute)
        {
            PlayerRoute = playerRoute;
        }
    }
}
