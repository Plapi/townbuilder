using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ResourceNodeData", menuName = "Scriptable Objects/Construction Data")]
    public class ConstructionData : EntityData
    {
        [TextArea(3, 8)] public string completedDescription;
        public List<CrafterRecipeInput> requiredResources;
    }
}
