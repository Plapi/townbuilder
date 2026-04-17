using System.Collections.Generic;
using UnityEngine;
using com.Plapamaru.Utilities;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Distribution
    {
        public readonly ResourceNode resourceNode;
        public readonly Extractor extractor;
        public readonly List<Conveyor> conveyors;
        public readonly Construction construction;

        public readonly List<Vector3> path;

        public Distribution(ResourceNode resourceNode, Extractor extractor, List<Conveyor> conveyors, Construction construction)
        {
            this.resourceNode = resourceNode;
            this.extractor = extractor;
            this.conveyors = conveyors;
            this.construction = construction;
            path = new List<Vector3>();

            CreateDistributionPath();
        }

        private void CreateDistributionPath()
        {
            var closest = Utils.GetClosest(extractor.ResourceOutput, conveyors[0].ResourceInputs);
            path.Add(closest.position);

            for (int i = 1; i < conveyors.Count; i++)
            {
                closest = Utils.GetClosest(closest, conveyors[i].ResourceInputs);
                path.Add(closest.position);
            }

            var farthest = Utils.GetFarthest(closest, conveyors[^1].ResourceInputs);
            path.Add(farthest.position);
        }
    }
}