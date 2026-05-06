using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : FactoryEntity<EntityData, EntitySaveData>
    {
        [SerializeField] private ResourceItemType _outputResourceType;

        public ResourceItemType OutputResourceType => _outputResourceType;
    }
}