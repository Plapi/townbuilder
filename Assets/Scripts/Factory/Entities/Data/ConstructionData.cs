using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ResourceNodeData", menuName = "Scriptable Objects/Construction Data")]
    public class ConstructionData : EntityData
    {
        public List<CrafterRecipeInput> requiredResources;
    }
}