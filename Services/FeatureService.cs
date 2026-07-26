using ModernBetaPositioningSystem.Models;

namespace ModernBetaPositioningSystem.Services
{
    public class FeatureService
    {
        private Dictionary<Guid, Feature> _features;
        public FeatureService()
        {
            _features = new Dictionary<Guid, Feature>();
        }
        public void AddFeature(Feature feature)
        {
            if (!_features.ContainsKey(feature.Id))
            {
                _features.Add(feature.Id, feature);
            }
        }
        public Feature GetClosest(Position position)
        {
            if (_features.Count == 0) return null;

            Feature closestFeature = null;
            double minDistance = double.MaxValue;

            foreach (var feature in _features.Values)
            {
                double distance = feature.DistanceFrom(position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestFeature = feature;
                }
            }
            return closestFeature;

        }
    }
}
