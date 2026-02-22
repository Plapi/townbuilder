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
        
        Vector2Int right = entity.Right;
        Vector2Int forward = entity.Forward;
        Vector2Int origin = entity.GridPos;

        var offset = Vector2Int.zero;
        var angleY = Mathf.RoundToInt(entity.transform.localEulerAngles.y);
        if (angleY == 90)
            offset = new Vector2Int(0, -1);
        else if (angleY == 180)
            offset = new Vector2Int(-1, -1);
        else if (angleY == -90 || angleY == 270)
            offset = new Vector2Int(0, 1);
        origin += offset;
        
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
        
        entity.ApplyCorrectPlacement(hasCorrectPlacement);
        
        if (_showDebugCells)
            PlaceDebugCells();
    }
    
    public void Release(FactoryEntity entity)
    {
        foreach (var gridPos in entity.GridPositions)
            _entities.Remove(gridPos);
        entity.GridPositions.Clear();
        
        if (_showDebugCells)
            PlaceDebugCells();
    }
    
    public void ConfirmPlacement(FactoryEntity entity)
    {
        entity.SetColor(Color.white);
    }
    
    private void PlaceDebugCells()
    {
        foreach (var debugCell in _debugCells)
            debugCell.gameObject.SetActive(false);
        
        var index = 0;
        foreach (var kvp in _entities)
        {
            if (index == _debugCells.Count)
                _debugCells.Add(Instantiate(_debugCell, transform));
            var debugCell = _debugCells[index];
            debugCell.name = $"DebugCell{index}_{kvp.Key}";
            debugCell.transform.position = new Vector3(kvp.Key.x, 0f, kvp.Key.y);
            debugCell.gameObject.SetActive(true);
            index++;
        }
    }
}