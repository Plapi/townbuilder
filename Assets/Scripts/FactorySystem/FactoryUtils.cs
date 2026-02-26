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
        var angleY = Mathf.RoundToInt(entity.transform.eulerAngles.y);
        var offset = GetOffset(angleY);
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
        if (Utils.TryGetMouseWorldPosition(camera, 1 << Constants.GroundLayer, Constants.RAY_MAX_DISTANCE, out var worldPos))
        {
            gridPos = WorldToGrid(worldPos, RoundType.Floor);
            return true;
        }
        return false;
    }

    public static EntityDirection GetDirection(Vector2Int from, Vector2Int to)
    {
        var dif = to - from;
        if (dif.x == 0 && dif.y < 0)
            return EntityDirection.Front;
        if (dif.x == 0 && dif.y > 0)
            return EntityDirection.Back;
        if (dif.x < 0 && dif.y == 0)
            return EntityDirection.Right;
        if (dif.x > 0 && dif.y == 0)
            return EntityDirection.Left;
        
        throw new Exception("Direction not found");
    }
    
    public static int GetAngle(EntityDirection direction)
    {
        return direction switch
        {
            EntityDirection.Front => 0,
            EntityDirection.Back => 180,
            EntityDirection.Right => 90,
            EntityDirection.Left => -90,
            _ => 0
        };
    }
}

public enum RoundType
{
    Floor,
    Round
}
