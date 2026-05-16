namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : FactoryEntity<ResourceNodeData, EntitySaveData>
    {
        public ResourceItemType OutputResourceType => ((ResourceNodeData)Data).resourceItemData.type;
    }
}