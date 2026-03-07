using System;
using System.Collections.Generic;
using com.Plapamaru.Utilities;
using com.Plapamaru.Pooling;
using com.Plapamaru.TownCrafter.Layers;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySystem : MonoBehaviour
    {
        [SerializeField] private FactorySaveSystem _saveSystem;
        [SerializeField] private FactorySimulationSystem _simulationSystem;
        [SerializeField] private DebugCells _debugCells;

        private readonly Dictionary<Vector2Int, Entity> _entities = new Dictionary<Vector2Int, Entity>();
        private readonly List<IFactoryListener> _listeners = new List<IFactoryListener>();

        private void Start()
        {
            SetStaticEntities();
            SetSaveEntities();
            _listeners.Add(_simulationSystem);
        }

        private void SetStaticEntities()
        {
            var staticEntities = GetComponentsInChildren<Entity>();
            foreach (var entity in staticEntities)
                SetEntities(entity);
        }

        private void SetSaveEntities()
        {
            var saveData = _saveSystem.Load();

            InstantiateSaveEntities<EntitySaveData, Extractor>(saveData.extractors);
            var conveyors = InstantiateSaveEntities<ConveyorSaveData, Conveyor>(saveData.conveyors);

            for (var i = 0; i < saveData.conveyors.Count; i++)
            {
                if (saveData.conveyors[i].nextConveyorGridPos != null)
                {
                    var gridPos = saveData.conveyors[i].nextConveyorGridPos.Value;
                    if (TryGetEntity(gridPos, out Conveyor nextConveyor))
                        conveyors[i].Connect(nextConveyor);
                    else
                        Debug.LogError($"Failed to find conveyor grid pos at {gridPos}");
                    conveyors[i].SetBeltDirection(saveData.conveyors[i].beltDirection);
                }
            }
        }

        public bool HasEntity(Vector2Int gridPos)
        {
            return _entities.ContainsKey(gridPos);
        }

        public bool TryGetEntity<T>(Vector2Int gridPos, out T entity) where T : Entity
        {
            if (_entities.TryGetValue(gridPos, out var baseEntity) && baseEntity is T typedEntity)
            {
                entity = typedEntity;
                return true;
            }
            entity = null;
            return false;
        }

        public Entity GetEntity(Vector2Int gridPos)
        {
            return TryGetEntity(gridPos, out Entity entity) ? entity : null;
        }

        public T InstantiateEntity<T>(string id) where T : Entity
        {
            return ObjectPoolingSystem.Instance.GetObject<T>(id, transform);
        }

        private List<U> InstantiateSaveEntities<T, U>(List<T> entitiesSaves)
            where T : EntitySaveData
            where U : Entity
        {
            var entities = new List<U>();
            foreach (var entitySave in entitiesSaves)
            {
                var entity = InstantiateEntity<U>(entitySave.id);
                entity.transform.SetAngleY(entitySave.rotationY);
                entity.SetLayer(LayerType.Environment);
                Place(entity, entitySave.gridPos);
                entities.Add(entity);
            }
            return entities;
        }

        public void PlaceOnCenter(Entity entity, Vector3 worldPos)
        {
            entity.SnapToGridOnCenter(worldPos);
            SetEntities(entity);
        }

        public void PlaceOnCenter(Entity entity, Vector2Int gridPos)
        {
            Vector2Int right = entity.Right;
            Vector2Int forward = entity.Forward;
            Vector2Int halfSize = new Vector2Int((entity.Size.x - 1) / 2, (entity.Size.y - 1) / 2);
            Vector2Int origin = gridPos - right * halfSize.x - forward * halfSize.y;
            Place(entity, origin);
        }

        public void Place(Entity entity, Vector2Int gridPos)
        {
            entity.SnapToGrid(gridPos);
            SetEntities(entity);
        }

        public void Rotate(Entity entity, int rotAngleY)
        {
            entity.Rotate(rotAngleY);
            SetEntities(entity);
        }

        public void MakeConveyorsConnexions(Conveyor from, Conveyor to, Action<Conveyor, Conveyor> onConveyorReplaced)
        {
            var fromPrev = from.PrevConveyor;
            if (fromPrev != null && FactoryUtils.AreDiagonals(fromPrev.GridPos, to.GridPos))
            {
                var inDir = from.GridPos - fromPrev.GridPos;
                var outDir = to.GridPos - from.GridPos;

                var newFromConveyor = InstantiateEntity<ConveyorCorner>(FactoryConstants.CONVEYOR_CORNER_NAME);
                Replace(from, newFromConveyor);
                newFromConveyor.ReleaseHighlightObject();
                fromPrev.Connect(newFromConveyor);

                newFromConveyor.transform.SetLocalAngleY(ConveyorHelper.GetCornerAngle(inDir, outDir, out var speedSign));
                newFromConveyor.SetBeltDirection(speedSign);

                to.transform.SetLocalAngleY(ConveyorHelper.GetStraightAngle(to.GridPos - newFromConveyor.GridPos));

                onConveyorReplaced?.Invoke(from, newFromConveyor);
                from = newFromConveyor;
            }
            else
            {
                var angleY = ConveyorHelper.GetStraightAngle(to.GridPos - from.GridPos);
                from.transform.SetLocalAngleY(angleY);
                to.transform.SetLocalAngleY(angleY);
            }

            from.SnapToGrid(from.GridPos);
            to.SnapToGrid(to.GridPos);

            from.Connect(to);
        }

        public bool TryFindPath(Conveyor conveyor, Vector2Int gridPos, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();

            var fPath = GridPathfinder.FindPath(conveyor.GridPos, gridPos, _entities);
            if (fPath == null || fPath.Count == 0)
                return false;

            fPath.RemoveAt(0);

            foreach (var pos in fPath)
            {
                if (_entities.ContainsKey(pos))
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
                foreach (var entity in _entities)
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

        private void SetEntities(Entity entity)
        {
            foreach (var gridPos in entity.GridPositions)
                _entities.Remove(gridPos);
            entity.GridPositions.Clear();

            Vector2Int right = entity.Right;
            Vector2Int forward = entity.Forward;
            Vector2Int origin = entity.GridPos;

            for (int x = 0; x < entity.Size.x; x++)
            {
                for (int y = 0; y < entity.Size.y; y++)
                {
                    Vector2Int gridPos = origin + right * x + forward * y;
                    if (_entities.ContainsKey(gridPos) == false)
                    {
                        entity.GridPositions.Add(gridPos);
                        _entities.Add(gridPos, entity);
                    }
                }
            }

            entity.OnPlacementUpdate(_entities);

            foreach (var listener in _listeners)
                listener.OnEntityPlaced(entity);

            _debugCells.UpdateDebugCells(_entities);
        }

        private void Replace(Conveyor replacedConveyor, Conveyor replacementConveyor)
        {
            Release(replacedConveyor);
            replacementConveyor.SetLayer(LayerType.Interactable);
            replacementConveyor.SnapToGrid(replacedConveyor.GridPos);
            SetEntities(replacementConveyor);
        }

        public void Release(FactoryEntity entity)
        {
            foreach (var gridPos in entity.GridPositions)
                _entities.Remove(gridPos);

            _debugCells.UpdateDebugCells(_entities);

            foreach (var listener in _listeners)
                listener.OnEntityRemoved(entity);

            ObjectPoolingSystem.Instance.ReleaseObject(entity);
        }

        public void SaveEntities()
        {
            _saveSystem.Save(_entities);
        }
    }
}