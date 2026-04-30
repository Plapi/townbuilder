using System.Collections.Generic;
using com.Plapamaru.Utilities;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ConveyorCorner : Conveyor
    {
        protected override List<Vector3> GetResourceDistributionPoints()
        {
            var farthest = Utils.GetFarthest(_resourceItem.transform, ResourceInputs);
            return SampleQuadraticBezier(
                _resourceItem.transform.position,
                CornerCurveControl(_resourceItem.transform.position, farthest.position),
                farthest.position,
                5);
        }

        private Vector3 CornerCurveControl(Vector3 from, Vector3 to)
        {
            var prev = PrevConveyor;
            if (prev != null)
            {
                var inDir = GridPos - prev.GridPos;
                if (inDir.x != 0)
                    return new Vector3(to.x, from.y, from.z);
                if (inDir.y != 0)
                    return new Vector3(from.x, from.y, to.z);
            }

            var center = new Vector3(GridPos.x + 0.5f, from.y, GridPos.y + 0.5f);
            var controlXFirst = new Vector3(to.x, from.y, from.z);
            var controlZFirst = new Vector3(from.x, from.y, to.z);
            var midX = QuadraticBezierPoint(from, controlXFirst, to, 0.5f);
            var midZ = QuadraticBezierPoint(from, controlZFirst, to, 0.5f);
            return (midX - center).sqrMagnitude <= (midZ - center).sqrMagnitude ? controlXFirst : controlZFirst;
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

        private static Vector3 QuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}