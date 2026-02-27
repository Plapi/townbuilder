using System.Collections.Generic;
using UnityEngine;

public class Extractor : FactoryEntity
{
    public override bool HasCorrectPlacement(Dictionary<Vector2Int, Entity> map)
    {
        if (base.HasCorrectPlacement(map) == false)
            return false;
        
        foreach (var input in _inputs)
        {
            var gridPos = FactoryUtils.GetGridPos(input);
            if (map.TryGetValue(gridPos, out var entity) && entity is MaterialEntity)
                return true;
        }
        
        return false;
    }
}
