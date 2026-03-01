using System;
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
    
    public bool TryGetEntity<T>(Vector2Int gridPos, out T entity) where T : Entity
    {
        if (_entities.TryGetValue(gridPos, out var baseEntity) && baseEntity is T typedEntity)
        {
            entity = typedEntity;
            return true;
        }
        entity = null;
        return false;
    }

    public T InstantiateEntity<T>(string id) where T : Entity
    {
        var entity = ObjectPoolingSystem.Instance.GetObject<T>(id, transform);
        return entity;
    }
    
    public void PlaceOnCenter(Entity entity, Vector3 worldPos)
    {
        entity.SnapToGridOnCenter(worldPos);
        SetEntities(entity);
    }
    
    public void Place(Entity entity, Vector2Int gridPos)
    {
        entity.SnapToGrid(gridPos);
        SetEntities(entity);
    }
    
    public void Rotate(Entity entity)
    {
        entity.Rotate();
        SetEntities(entity);
    }
    
    public void MakeConveyorsConnexions(Conveyor from, Conveyor to, Action<Conveyor, Conveyor> onConveyorReplaced)
    {
        var fromPrev = from.PrevConveyor;
        if (fromPrev != null && FactoryUtils.AreDiagonals(fromPrev.GridPos, to.GridPos))
        {
            var inDir  = from.GridPos - fromPrev.GridPos;
            var outDir = to.GridPos - from.GridPos;
            
            var newFromConveyor = InstantiateEntity<ConveyorCorner>(FactoryConstants.CONVEYOR_CORNER_NAME);
            Replace(from, newFromConveyor);
            fromPrev.Connect(newFromConveyor);
            
            newFromConveyor.transform.SetLocalAngleY(ConveyorHelper.GetCornerAngle(inDir, outDir, out var speedSign));
            newFromConveyor.SetSpeedSign(speedSign);
            
            to.transform.SetLocalAngleY(ConveyorHelper.GetStraightAngle(to.GridPos - newFromConveyor.GridPos));
            
            onConveyorReplaced?.Invoke(from, newFromConveyor);
            from = newFromConveyor;
        }
        else
        {
            var angleY = ConveyorHelper.GetStraightAngle(to.GridPos - from.GridPos);
            from.transform.SetLocalAngleY(angleY);
            to.transform.SetLocalAngleY(angleY);
        }
        
        from.SnapToGrid(from.GridPos);
        to.SnapToGrid(to.GridPos);
        
        from.Connect(to);
    }
    
    private void Replace(Conveyor replacedConveyor, Conveyor replacementConveyor)
    {
        Release(replacedConveyor);
        replacementConveyor.gameObject.SetLayerRecursively(Layers.InteractableLayer);
        replacementConveyor.SnapToGrid(replacedConveyor.GridPos);
        SetEntities(replacementConveyor);
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
        
        _debugCells.UpdateDebugCells(_entities);
        
        ObjectPoolingSystem.Instance.ReleaseObject(entity);
    }
}