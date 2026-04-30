using System.Collections.Generic;
using com.Plapamaru.Utilities;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ConveyorStraight : Conveyor
    {
        protected override List<Vector3> GetResourceDistributionPoints()
        {
            var farthest = Utils.GetFarthest(_resourceItem.transform, ResourceInputs);
            return new List<Vector3>() { _resourceItem.transform.position, farthest.position };
        }
    }
}