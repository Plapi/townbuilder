using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : FactoryEntity<EntityData, EntitySaveData>
    {
        [Space]
        [SerializeField] private ResourceItemData _outputResourceData;

        public ResourceItemType OutputResourceType => _outputResourceData.type;
    }
}