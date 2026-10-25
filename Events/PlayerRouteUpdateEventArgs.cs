using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerRouteUpdateEventArgs(PlayerRoute PlayerRoute, bool IsApproaching, bool IsLeaving, bool IsInside);
