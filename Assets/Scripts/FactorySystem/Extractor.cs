using System.Collections.Generic;
using UnityEngine;

public class Extractor : FactoryEntity
{
    public override bool HasNecessaryConnexion(Dictionary<Vector2Int, Entity> map)
    {
        foreach (var input in _inputs)
        {
            var gridPos = Utils.WorldToGrid(input);
            if (map.TryGetValue(gridPos, out var entity) && entity is MaterialEntity)
                return true;
        }
        
        return false;
    }
}
