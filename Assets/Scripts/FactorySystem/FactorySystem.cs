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
        entity.SetColor(Color.white);
    }
}