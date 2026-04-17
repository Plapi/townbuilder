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
                var points = conveyors[i] is ConveyorCorner corner ?
                    SampleQuadraticBezier(
                        closest.position,
                        CornerCurveControl(corner, closest.position, closest1.position),
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

        private static Vector3 CornerCurveControl(ConveyorCorner corner, Vector3 from, Vector3 to)
        {
            var prev = corner.PrevConveyor;
            if (prev != null)
            {
                var inDir = corner.GridPos - prev.GridPos;
                if (inDir.x != 0)
                    return new Vector3(to.x, from.y, from.z);
                if (inDir.y != 0)
                    return new Vector3(from.x, from.y, to.z);
            }

            var center = new Vector3(corner.GridPos.x + 0.5f, from.y, corner.GridPos.y + 0.5f);
            var controlXFirst = new Vector3(to.x, from.y, from.z);
            var controlZFirst = new Vector3(from.x, from.y, to.z);
            var midX = QuadraticBezierPoint(from, controlXFirst, to, 0.5f);
            var midZ = QuadraticBezierPoint(from, controlZFirst, to, 0.5f);
            return (midX - center).sqrMagnitude <= (midZ - center).sqrMagnitude ? controlXFirst : controlZFirst;
        }

        private static Vector3 QuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
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
                list.Add(QuadraticBezierPoint(p0, p1, p2, t));
            }

            return list;
        }

        public bool IsStillValid()
        {
            if (!extractor.gameObject.activeSelf)
                return false;
            
            foreach (var conveyor in conveyors)
                if (!conveyor.gameObject.activeSelf)
                    return false;

            return true;
        }
    }
}