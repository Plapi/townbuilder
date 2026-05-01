using com.Plapamaru.TownCrafter.Layers;
using UnityEngine;
using Vector2Int = UnityEngine.Vector2Int;

namespace com.Plapamaru.TownCrafter.Factory
{
    public static class FactoryUtils
    {
        public static Vector2Int WorldToGrid(Vector3 worldPos, RoundType roundType)
        {
            return roundType == RoundType.Floor ?
                new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z)) :
                new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        public static void PlaceToGrid(Entity entity)
        {
            var offset = GetOffset(entity.AngleY);
            entity.transform.position = new Vector3(entity.GridPos.x + offset.x, 0f, entity.GridPos.y + offset.y);
        }

        public static Vector2Int GetGridPos(Transform transform)
        {
            var angleY = Mathf.RoundToInt(transform.eulerAngles.y);
            var gridPos = WorldToGrid(transform.position, RoundType.Round);
            return gridPos - GetOffset(angleY);
        }

        private static Vector2Int GetOffset(int angleY)
        {
            return angleY switch
            {
                90 => new Vector2Int(0, 1),
                180 => new Vector2Int(1, 1),
                -90 => new Vector2Int(1, 0),
                270 => new Vector2Int(1, 0),
                _ => Vector2Int.zero
            };
        }

        public static bool TryGetMouseGridPosition(Camera camera, out Vector2Int gridPos)
        {
            gridPos = Vector2Int.zero;
            if (LayersUtils.Raycast(camera, LayerType.Ground, out var hit))
            {
                gridPos = WorldToGrid(hit.point, RoundType.Floor);
                return true;
            }
            return false;
        }

        public static bool AreNeighbour(Vector2Int a, Vector2Int b)
        {
            return AreAdjacent(a, b) || AreDiagonals(a, b);
        }

        public static bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx + dy == 1;
        }

        public static bool AreDiagonals(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx == 1 && dy == 1;
        }

        public static bool TryGetInputFeedOutDir(Transform matchedInput, out Vector2Int outDir)
        {
            outDir = default;
            if (matchedInput == null)
                return false;
            if (TryWorldDirToUnitCardinal(-matchedInput.forward, out outDir))
                return true;
            return TryWorldDirToUnitCardinal(matchedInput.forward, out outDir);
        }

        private static bool TryWorldDirToUnitCardinal(Vector3 worldDir, out Vector2Int cardinal)
        {
            cardinal = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(worldDir.x), -1, 1),
                Mathf.Clamp(Mathf.RoundToInt(worldDir.z), -1, 1));
            var ax = Mathf.Abs(cardinal.x);
            var ay = Mathf.Abs(cardinal.y);
            if (ax + ay == 0)
                return false;
            if (ax != 0 && ay != 0)
                cardinal = ax >= ay ? new Vector2Int((int)Mathf.Sign(cardinal.x), 0) : new Vector2Int(0, (int)Mathf.Sign(cardinal.y));
            return Mathf.Abs(cardinal.x) + Mathf.Abs(cardinal.y) == 1;
        }
    }

    public enum RoundType
    {
        Floor,
        Round
    }
}