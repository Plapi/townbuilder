using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public interface IFactorySystem
    {
        bool HasEntity(Vector2Int gridPos);
        bool TryGetEntity<T>(Vector2Int gridPos, out T entity) where T : Entity;
        Entity GetEntity(Vector2Int gridPos);
        T InstantiateEntity<T>(string id) where T : Entity;
        void PlaceOnCenter(Entity entity, Vector3 worldPos);
        void PlaceOnCenter(Entity entity, Vector2Int gridPos);
        void Place(Entity entity, Vector2Int gridPos);
        void Rotate(Entity entity, int rotAngleY);
        void MakeConveyorsConnexions(Conveyor from, Conveyor to, Action<Conveyor, Conveyor> onConveyorReplaced);
        bool TryFindPath(Conveyor conveyor, Vector2Int gridPos, out List<Vector2Int> path);
        void Release(FactoryEntity entity);
    }
}
