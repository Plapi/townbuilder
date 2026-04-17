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

        public readonly DistributionPath path;

        public Distribution(ResourceNode resourceNode, Extractor extractor, List<Conveyor> conveyors, Construction construction)
        {
            this.resourceNode = resourceNode;
            this.extractor = extractor;
            this.conveyors = conveyors;
            this.construction = construction;
            path = new DistributionPath();

            CreateDistributionPath();
        }

        private void CreateDistributionPath()
        {
            var closest = Utils.GetClosest(extractor.ResourceOutput, conveyors[0].ResourceInputs);
            path.PointGroups.Add(new DistributionPath.PointGroup(closest.position));

            for (int i = 0; i < conveyors.Count - 1; i++)
            {
                var closest1 = Utils.GetClosest(closest, conveyors[i + 1].ResourceInputs);
                var points = conveyors[i] is ConveyorCorner ?
                    SampleQuadraticBezier(
                        closest.position,
                        CornerCurveControl(closest.position, closest1.position),
                        closest1.position,
                        5) :
                    new List<Vector3>() { closest.position, closest1.position };

                path.PointGroups.Add(new DistributionPath.PointGroup(points));

                closest = closest1;
            }

            path.PointGroups.Add(new DistributionPath.PointGroup(new List<Vector3>()
            {
                closest.position,
                Utils.GetFarthest(closest, conveyors[^1].ResourceInputs).position
            }));
        }

        private static Vector3 CornerCurveControl(Vector3 from, Vector3 to)
        {
            var d = to - from;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.z))
                return new Vector3(to.x, from.y, from.z);
            return new Vector3(from.x, from.y, to.z);
        }

        private static List<Vector3> SampleQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, int sampleCount)
        {
            var list = new List<Vector3>(sampleCount);
            if (sampleCount < 2)
            {
                list.Add(p0);
                return list;
            }

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var u = 1f - t;
                list.Add(u * u * p0 + 2f * u * t * p1 + t * t * p2);
            }

            return list;
        }
    }
}