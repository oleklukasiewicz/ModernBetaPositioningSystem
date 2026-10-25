using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Events;

public record PlayerRouteFeatureLeavingEventArgs(PlayerRoute PlayerRoute, Feature LeavingFeature);
