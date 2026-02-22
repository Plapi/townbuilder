using System.Collections.Generic;
using UnityEngine;

public class MaterialEntity : Entity
{
    public override bool HasNecessaryConnexion(Dictionary<Vector2Int, Entity> map)
    {
        return true;
    }
}
