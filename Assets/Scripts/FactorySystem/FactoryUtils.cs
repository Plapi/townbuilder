using System;
using UnityEngine;
using Vector2Int = UnityEngine.Vector2Int;

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
        if (Layers.Raycast(camera, Layers.GroundLayer, out var hit))
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
}

public enum RoundType
{
    Floor,
    Round
}
