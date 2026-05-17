namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : FactoryEntity<ResourceNodeData, EntitySaveData>
    {
        public ResourceItemType OutputResourceType => Data.resourceItemData.type;
    }
}