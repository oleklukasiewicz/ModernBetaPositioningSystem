using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerRouteHeadingChangedEventargs(PlayerRoute PlayerRoute, Feature? PreviousHeading);
