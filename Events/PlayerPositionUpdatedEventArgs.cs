using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerPositionUpdatedEventArgs(PlayerPosition PlayerPosition, PlayerPosition? PreviousPlayerPosition);
