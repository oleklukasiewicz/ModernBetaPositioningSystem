using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerPositionAddedEventArgs:EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }

        public PlayerPositionAddedEventArgs(PlayerPosition playerPosition)
        {
            PlayerPosition = playerPosition;
        }
    }
}
