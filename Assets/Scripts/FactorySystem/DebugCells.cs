using System.Collections.Generic;
using UnityEngine;

public class DebugCells : MonoBehaviour
{
    [SerializeField] private Transform _debugCellEntity;
    [SerializeField] private Transform _debugCellInput;
    [SerializeField] private bool _showDebugCells;
    
    private readonly List<Transform> _debugCellsEntities = new List<Transform>();
    private readonly List<Transform> _debugCellsInputs = new List<Transform>();
    
    public void UpdateDebugCells(Dictionary<Vector2Int, Entity> entities)
    {
        if (_showDebugCells == false)
            return;
        
        foreach (var debugCell in _debugCellsEntities)
            debugCell.gameObject.SetActive(false);
        foreach (var debugCellInput in _debugCellsInputs)
            debugCellInput.gameObject.SetActive(true);
        
        var factoryEntities = new List<FactoryEntity>();
        
        var index = 0;
        foreach (var kvp in entities)
        {
            InstantiateDebugCell(index, _debugCellEntity, _debugCellsEntities, kvp.Key);
            index++;
            
            if (kvp.Value is FactoryEntity factoryEntity && factoryEntities.Contains(factoryEntity) == false)
                factoryEntities.Add(factoryEntity);
        }
        
        index = 0;
        foreach (var factoryEntity in factoryEntities)
        {
            foreach (var input in factoryEntity.Inputs)
            {
                InstantiateDebugCell(index, _debugCellInput, _debugCellsInputs, FactoryUtils.GetGridPos(input));
                index++;
            }
        }
    }

    private void InstantiateDebugCell(int index, Transform debugCell, List<Transform> pool, Vector2Int gridPos)
    {
        if (index == pool.Count)
            pool.Add(Instantiate(debugCell, debugCell.transform.parent));
        debugCell = pool[index];
        debugCell.gameObject.SetActive(true);
        debugCell.name = $"{debugCell.name}{index}_{gridPos}";
        debugCell.transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
    }
}
