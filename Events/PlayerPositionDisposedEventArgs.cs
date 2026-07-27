using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerPositionDisposedEventArgs: EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }

        public PlayerPositionDisposedEventArgs(PlayerPosition playerPosition)
        {
            PlayerPosition = playerPosition;
        }
    }
}
