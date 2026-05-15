using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ResourceNodeData", menuName = "Scriptable Objects/Resource Node Data")]
    public class ResourceNodeData : EntityData
    {
        public ResourceItemData resourceItemData;
    }
}