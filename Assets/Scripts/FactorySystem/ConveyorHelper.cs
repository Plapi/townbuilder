using UnityEngine;

public static class ConveyorHelper
{
    public static int GetStraightAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.right)
            return 90;
        if (dir == Vector2Int.down)
            return 180;
        if (dir == Vector2Int.left)
            return -90;
        return 0;
    }
    
    public static int GetCornerAngle(Vector2Int inDir, Vector2Int outDir, out int speedSign)
    {
        speedSign = 1;
        inDir = new Vector2Int(Mathf.Clamp(inDir.x, -1, 1), Mathf.Clamp(inDir.y, -1, 1));
        outDir = new Vector2Int(Mathf.Clamp(outDir.x, -1, 1), Mathf.Clamp(outDir.y, -1, 1));

        if (inDir == Vector2Int.up && outDir == Vector2Int.right)
            return 0;
        
        if (inDir == Vector2Int.left && outDir == Vector2Int.down)
        {
            speedSign = -1;
            return 0;
        }
        
        if (inDir == Vector2Int.right && outDir == Vector2Int.down)
            return 90;
        
        if (inDir == Vector2Int.up && outDir == Vector2Int.left)
        {
            speedSign = -1;
            return 90;
        }

        if (inDir == Vector2Int.down && outDir == Vector2Int.left)
            return 180;

        if (inDir == Vector2Int.right && outDir == Vector2Int.up)
        {
            speedSign = -1;
            return 180;
        }

        if (inDir == Vector2Int.left && outDir == Vector2Int.up)
            return -90;

        if (inDir == Vector2Int.down && outDir == Vector2Int.right)
        {
            speedSign = -1;
            return -90;
        }
        
        Debug.LogError($"Invalid corner combination {inDir} → {outDir}");
        return 0;
    }
}
