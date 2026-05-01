using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using com.Plapamaru.Singletons;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactoryMap : MonoBehaviourSingleton<FactoryMap>
    {
        [SerializeField] private DebugCells _debugCells;

        public readonly Dictionary<Vector2Int, Entity> Map = new Dictionary<Vector2Int, Entity>();

        private CancellationToken _externalCT;

        public void Init(CancellationToken externalCT)
        {
            _externalCT = externalCT;
        }

        public void Add(Entity entity, Vector2Int gridPos)
        {
            entity.GridPositions.Add(gridPos);
            Map.Add(gridPos, entity);
        }

        public void Remove(Entity entity)
        {
            foreach (var gridPos in entity.GridPositions)
                Map.Remove(gridPos);
            entity.GridPositions.Clear();
        }

        public bool HasEntity(Vector2Int gridPos)
        {
            return Map.ContainsKey(gridPos);
        }

        public bool TryGetEntity<T>(Vector2Int gridPos, out T entity) where T : Entity
        {
            if (Map.TryGetValue(gridPos, out var baseEntity) && baseEntity is T typedEntity)
            {
                entity = typedEntity;
                return true;
            }
            entity = null;
            return false;
        }

        public bool TryGetFactoryEntityFromInput(Vector2Int gridPos, out FactoryEntity entityOut,
            out Transform matchedInputOut)
        {
            entityOut = null;
            matchedInputOut = null;
            var seen = new HashSet<Entity>();
            foreach (var kv in Map)
            {
                var entity = kv.Value;
                if (!seen.Add(entity) || entity is not FactoryEntity factoryEntity || !IsConveyorFeedTarget(factoryEntity))
                    continue;

                foreach (var input in factoryEntity.Inputs)
                {
                    if (input == null)
                        continue;
                    if (FactoryUtils.GetGridPos(input) == gridPos)
                    {
                        entityOut = factoryEntity;
                        matchedInputOut = input;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsConveyorFeedTarget(FactoryEntity entity)
        {
            return entity is Construction or Crafter;
        }

        public T InstantiateEntity<T>(string id) where T : Entity
        {
            var entity = ObjectPoolingSystem.Instance.GetObject<T>(id, transform);

            UniTask.Action(async () =>
            {
                await UniTask.NextFrame(_externalCT);
                entity.Init(_externalCT);
            }).Invoke();

            return entity;
        }

        public bool IsDiagonalWithPossibleExtractorConnexion(Conveyor from, Conveyor to, out Vector2Int fromPrevGridPos)
        {
            fromPrevGridPos = default;

            if (from.PrevConveyor != null)
                return false;

            foreach (var entity in Map)
            {
                if (entity.Value is not Extractor extractor)
                    continue;

                foreach (var output in entity.Value.Outputs)
                {
                    var outputGridPos = FactoryUtils.GetGridPos(output);
                    if (outputGridPos != from.GridPos)
                        continue;

                    var adjGridPositions = from.GetAdjacentGridPositions();
                    foreach (var adjGridPosition in adjGridPositions)
                        if (Map.ContainsKey(adjGridPosition) && Map[adjGridPosition] == extractor &&
                            FactoryUtils.AreDiagonals(adjGridPosition, to.GridPos))
                        {
                            fromPrevGridPos = adjGridPosition;
                            return true;
                        }
                }
            }

            return false;
        }

        public bool TryFindPath(Conveyor conveyor, Vector2Int gridPos, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();

            var fPath = GridPathfinder.FindPath(conveyor.GridPos, gridPos, Map);
            if (fPath == null || fPath.Count == 0)
                return false;

            fPath.RemoveAt(0);

            foreach (var pos in fPath)
            {
                if (Map.ContainsKey(pos))
                    break;
                path.Add(pos);
            }

            return true;
        }

        public void SetActiveInputsOutputs(params (bool isInput, bool activate, Type type)[] items)
        {
            foreach (var item in items)
            {
                var updatedEntities = new List<Entity>();
                foreach (var entity in Map)
                {
                    if (!updatedEntities.Contains(entity.Value) && entity.Value.GetType() == item.type)
                    {
                        updatedEntities.Add(entity.Value);
                        if (item.isInput)
                            entity.Value.SetActiveInputs(item.activate);
                        else
                            entity.Value.SetActiveOutputs(item.activate);
                    }
                }
            }
        }

        public void UpdateDebugCells()
        {
            _debugCells.UpdateDebugCells(Map);
        }
    }
}