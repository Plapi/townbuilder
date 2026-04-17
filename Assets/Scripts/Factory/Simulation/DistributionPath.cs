using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class DistributionPath
    {
        public readonly List<PointGroup> PointGroups = new List<PointGroup>();

        public class PointGroup
        {
            public readonly List<Vector3> points;

            public PointGroup(List<Vector3> points)
            {
                this.points = points;
            }

            public PointGroup(Vector3 position)
            {
                points = new List<Vector3> { position };
            }
        }
    }
}