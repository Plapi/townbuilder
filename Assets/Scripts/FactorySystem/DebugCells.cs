using System.Collections.Generic;
using UnityEngine;

public class DebugCells : MonoBehaviour
{
    [SerializeField] private Transform _debugCellEntity;
    [SerializeField] private Transform _debugCellInput;
    [SerializeField] private Transform _debugCellOutput;
    [SerializeField] private bool _showDebugCells;
    [SerializeField] private bool _showDebugCellsInputs;
    [SerializeField] private bool _showDebugCellsOutputs;
    
    private readonly List<Transform> _debugCellsEntities = new List<Transform>();
    private readonly List<Transform> _debugCellsInputs = new List<Transform>();
    private readonly List<Transform> _debugCellsOutputs = new List<Transform>();
    private readonly List<FactoryEntity> _factoryEntities = new List<FactoryEntity>();
    
    public void UpdateDebugCells(Dictionary<Vector2Int, Entity> entities)
    {
        if (_showDebugCells == false)
            return;

        UpdateDebugCellsEntities(entities);
        
        if (_showDebugCellsInputs)
            UpdateDebugCellsInputs();
        
        if (_showDebugCellsOutputs)
            UpdateDebugCellsOutputs();
    }
    
    private void UpdateDebugCellsEntities(Dictionary<Vector2Int, Entity> entities)
    {
        foreach (var debugCell in _debugCellsEntities)
            debugCell.gameObject.SetActive(false);
        
        _factoryEntities.Clear();
        
        var index = 0;
        foreach (var kvp in entities)
        {
            InstantiateDebugCell(index, _debugCellEntity, _debugCellsEntities, kvp.Key);
            index++;
            
            if (kvp.Value is FactoryEntity factoryEntity && _factoryEntities.Contains(factoryEntity) == false)
                _factoryEntities.Add(factoryEntity);
        }
    }
    
    private void UpdateDebugCellsInputs()
    {
        foreach (var debugCellInput in _debugCellsInputs)
            debugCellInput.gameObject.SetActive(false);
        
        var index = 0;
        foreach (var factoryEntity in _factoryEntities)
        {
            foreach (var input in factoryEntity.Inputs)
            {
                InstantiateDebugCell(index, _debugCellInput, _debugCellsInputs, FactoryUtils.GetGridPos(input));
                index++;
            }
        }
    }

    private void UpdateDebugCellsOutputs()
    {
        foreach (var debugCellOutput in _debugCellsOutputs)
            debugCellOutput.gameObject.SetActive(false);
        
        var index = 0;
        foreach (var factoryEntity in _factoryEntities)
        {
            foreach (var output in factoryEntity.Outputs)
            {
                InstantiateDebugCell(index, _debugCellOutput, _debugCellsOutputs, FactoryUtils.GetGridPos(output));
                index++;
            }
        }
    }
    
    private static void InstantiateDebugCell(int index, Transform debugCell, List<Transform> pool, Vector2Int gridPos)
    {
        if (index == pool.Count)
            pool.Add(Instantiate(debugCell, debugCell.transform.parent));
        debugCell = pool[index];
        debugCell.gameObject.SetActive(true);
        debugCell.name = $"{debugCell.name}{index}_{gridPos}";
        debugCell.transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
    }
}
