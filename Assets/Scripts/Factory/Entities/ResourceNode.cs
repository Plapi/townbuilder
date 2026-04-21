using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : Entity
    {
        [SerializeField] private ResourceItemType _outputResourceType;

        public ResourceItemType OutputResourceType => _outputResourceType;

        public List<Extractor> GetConnectedExtractors()
        {
            var extractors = new List<Extractor>();
            var adjPositions = GetAdjacentGridPositions();
            foreach (var gridPos in adjPositions)
                if (FactoryMap.Instance.TryGetEntity(gridPos, out Extractor extractor) && !extractors.Contains(extractor))
                    extractors.Add(extractor);
            return extractors;
        }
    }
}