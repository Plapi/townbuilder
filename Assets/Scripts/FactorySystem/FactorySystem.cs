using System.Collections.Generic;
using UnityEngine;

public class FactorySystem : MonoBehaviourSingleton<FactorySystem>
{
    [SerializeField] private DebugCells _debugCells;
    
    private readonly Dictionary<Vector2Int, Entity> _entities = new Dictionary<Vector2Int, Entity>();
    
    private void Start()
    {
        var materialEntities = GetComponentsInChildren<MaterialEntity>();
        foreach (var materialEntity in materialEntities)
            SetEntities(materialEntity);
    }
    
    public bool HasEntity(Vector2Int gridPos)
    {
        return _entities.ContainsKey(gridPos);
    }
    
    public void PlaceOnCenter(Entity entity, Vector3 worldPos)
    {
        entity.SnapToGridOnCenter(worldPos);
        SetEntities(entity);
    }
    
    public void Place(Entity entity, Vector2Int gridPos)
    {
        var prevGridPos = entity.GridPos;
        entity.SnapToGrid(gridPos);
        
        if (prevGridPos != entity.GridPos)
            SetEntities(entity);
    }

    public void Rotate(Entity entity)
    {
        entity.Rotate();
        SetEntities(entity);
    }
    
    public void MakeConveyorsConnexions(Conveyor from, Conveyor to)
    {
        var fromPrev = from.PrevConveyor;
        if (fromPrev != null && FactoryUtils.AreDiagonals(fromPrev.GridPos, to.GridPos))
        {
            var fromGridPos = from.GridPos;
            var inDir  = from.GridPos - fromPrev.GridPos;
            var outDir = to.GridPos - from.GridPos;
            
            Release(from);
            ObjectPoolingSystem.Instance.ReleaseObject(from);
            
            from = ObjectPoolingSystem.Instance.GetObject<ConveyorCorner>("ConveyorCorner");
            from.gameObject.SetLayerRecursively(Layers.InteractableLayer);
            fromPrev.Connect(from);
            Place(from, fromGridPos);
            
            from.transform.SetLocalAngleY(GetCornerAngle(inDir, outDir, out var speedSign));
            ((ConveyorCorner)from).SetSpeedSign(speedSign);
            
            to.transform.SetLocalAngleY(GetStraightAngle(to.GridPos - from.GridPos));
        }
        else
        {
            var angleY = GetStraightAngle(to.GridPos - from.GridPos);
            from.transform.SetLocalAngleY(angleY);
            to.transform.SetLocalAngleY(angleY);
        }
        
        from.SnapToGrid(from.GridPos);
        to.SnapToGrid(to.GridPos);
        
        from.Connect(to);
    }

    private static int GetStraightAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.right)
            return 90;
        if (dir == Vector2Int.down)
            return 180;
        if (dir == Vector2Int.left)
            return -90;
        return 0;
    }
    
    private static int GetCornerAngle(Vector2Int inDir, Vector2Int outDir, out int speedSign)
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
    
    private void SetEntities(Entity entity)
    {
        foreach (var gridPos in entity.GridPositions)
            _entities.Remove(gridPos);
        entity.GridPositions.Clear();
        
        Vector2Int right = entity.Right;
        Vector2Int forward = entity.Forward;
        Vector2Int origin = entity.GridPos;
        
        for (int x = 0; x < entity.Size.x; x++)
        {
            for (int y = 0; y < entity.Size.y; y++)
            {
                Vector2Int gridPos = origin + right * x + forward * y;
                if (_entities.ContainsKey(gridPos) == false)
                {
                    entity.GridPositions.Add(gridPos);
                    _entities.Add(gridPos, entity);    
                }
            }
        }
        
        entity.ApplyCorrectPlacement(entity.HasCorrectPlacement(_entities));
        
        _debugCells.UpdateDebugCells(_entities);
    }
    
    public void Release(FactoryEntity entity)
    {
        foreach (var gridPos in entity.GridPositions)
            _entities.Remove(gridPos);
        entity.GridPositions.Clear();
        
        _debugCells.UpdateDebugCells(_entities);
    }
    
    public void ConfirmPlacement(FactoryEntity entity)
    {
        
    }
}