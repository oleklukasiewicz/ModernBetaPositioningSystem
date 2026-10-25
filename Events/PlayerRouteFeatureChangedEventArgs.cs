using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerRouteFeatureChangedEventArgs(PlayerRoute PlayerRoute, Feature? PreviousFeature);
