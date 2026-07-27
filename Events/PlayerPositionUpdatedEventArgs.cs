using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerPositionUpdatedEventArgs : EventArgs
    {
        public PlayerPosition PlayerPosition { get; private set; }
        public PlayerPosition PreviousPlayerPosition { get; private set; }
        public PlayerPositionUpdatedEventArgs(PlayerPosition playerPosition, PlayerPosition previousPlayerPosition)
        {
            PlayerPosition = playerPosition;
            PreviousPlayerPosition = previousPlayerPosition;
        }
    }
}
