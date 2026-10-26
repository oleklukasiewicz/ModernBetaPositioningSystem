using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerPositionsFetchedEventArgs(List<PlayerPosition> Positions);
