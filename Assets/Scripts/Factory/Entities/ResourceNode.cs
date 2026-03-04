using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceNode : Entity
    {
        [SerializeField] private ResourceItemType _outputResourceType;
        
        public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
        {
            
        }
    }
}