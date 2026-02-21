using System.Collections.Generic;
using UnityEngine;

public class FactorySystem : MonoBehaviourSingleton<FactorySystem>
{
    [SerializeField] private Transform _debugCell;
    [SerializeField] private bool _showDebugCells;
    
    private readonly Dictionary<Vector2Int, FactoryEntity> _entities = new Dictionary<Vector2Int, FactoryEntity>();
    private readonly List<Transform> _debugCells = new List<Transform>();
    
    public void PlaceOnCenter(FactoryEntity entity, Vector3 worldPos)
    {
        entity.PlaceOnCenter(worldPos);
        SetEntities(entity);
    }
    
    public void Place(FactoryEntity entity, Vector3 worldPos)
    {
        var gridPos = Utils.WorldToGrid(worldPos);
        if (gridPos == entity.GridPos)
            return;
        
        entity.Place(gridPos);
        SetEntities(entity);
    }

    public void Rotate(FactoryEntity entity)
    {
        entity.Rotate();
        SetEntities(entity);
    }
    
    private void SetEntities(FactoryEntity entity)
    {
        foreach (var gridPos in entity.GridPositions)
            _entities.Remove(gridPos);
        entity.GridPositions.Clear();
        
        var hasCorrectPlacement = true;
        Vector2Int origin = entity.GridPos;
        Vector2Int right = entity.Right;
        Vector2Int forward = entity.Forward;
        
        for (int x = 0; x < entity.Size.x; x++)
        {
            for (int y = 0; y < entity.Size.y; y++)
            {
                Vector2Int gridPos = origin + right * x + forward * y;
                if (_entities.ContainsKey(gridPos))
                {
                    hasCorrectPlacement = false;
                    continue;
                }
                
                entity.GridPositions.Add(gridPos);
                _entities.Add(gridPos, entity);
            }
        }
        
        entity.SetColor(hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor);
        
        if (_showDebugCells)
            PlaceDebugCells();
    }
    
    private void PlaceDebugCells()
    {
        foreach (var debugCell in _debugCells)
            debugCell.gameObject.SetActive(false);
        
        var index = 0;
        foreach (var kvp in _entities)
        {
            if (index == _debugCells.Count)
            {
                var newDebugCll = Instantiate(_debugCell, transform);
                newDebugCll.name = $"DebugCell{index}";
                _debugCells.Add(newDebugCll);
            }
            var debugCell = _debugCells[index];
            debugCell.transform.position = new Vector3(kvp.Key.x, 0f, kvp.Key.y);
            debugCell.gameObject.SetActive(true);
            index++;
        }
    }
}