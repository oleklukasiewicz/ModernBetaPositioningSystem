using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events
{
    public class PlayerPositionsFetchedEventArgs: EventArgs
    {
        public List<PlayerPosition> Positions { get; private set; }
        public PlayerPositionsFetchedEventArgs(List<PlayerPosition> positions)
        {
            Positions = positions;
        }
    }
}
