using System.Collections.Generic;
using UnityEngine;

public class DebugCells : MonoBehaviour
{
    [SerializeField] private Transform _debugCell;
    [SerializeField] private bool _showDebugCells;
    
    private readonly List<Transform> _debugCells = new List<Transform>();
    
    public void UpdateDebugCells(Dictionary<Vector2Int, Entity> entities)
    {
        if (_showDebugCells == false)
            return;
        
        foreach (var debugCell in _debugCells)
            debugCell.gameObject.SetActive(false);
        
        var index = 0;
        foreach (var kvp in entities)
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
