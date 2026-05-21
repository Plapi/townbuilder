using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Utilities;
using com.Plapamaru.Pooling;
using com.Plapamaru.TownCrafter.Layers;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySystem : MonoBehaviour
    {
        [SerializeField] private FactorySaveSystem _saveSystem;

        public void Init(CancellationToken externalCT)
        {
            FactoryMap.Instance.Init(externalCT);

            InitEntities(externalCT);
        }

        public void OnBuildEnter()
        {
            SimulationClock.SetPaused(true);
        }

        public void OnBuildExit()
        {
            SimulationClock.SetPaused(false);
            Save();
        }

        public void Save()
        {
            _saveSystem.Save();
        }

        public void SetAllConstructionsToMaxStage()
        {
            var constructions = GetComponentsInChildren<Construction>(true);

            foreach (var construction in constructions)
                construction.SetToMaxStage();
        }

        private void InitEntities(CancellationToken externalCT)
        {
            var staticEntities = GetComponentsInChildren<Entity>();
            foreach (var entity in staticEntities)
                SetEntity(entity);

            var saveData = _saveSystem.Load();

            InstantiateSaveEntities<ExtractorSaveData, Extractor>(saveData.extractors);
            InstantiateSaveEntities<ConveyorSaveData, Conveyor>(saveData.conveyors);
            InstantiateSaveEntities<EntitySaveData, Crafter>(saveData.crafters);

            foreach (var constructionData in saveData.constructions)
            {
                if (!FactoryMap.Instance.TryGetEntity(constructionData.gridPos, out Construction construction))
                {
                    Debug.LogError($"Could not get construction at {constructionData.gridPos}");
                    continue;
                }

                construction.SetSaveData(constructionData);
            }

            foreach (var entity in staticEntities)
                entity.Init(externalCT);
        }

        private void InstantiateSaveEntities<T, U>(List<T> entitiesSaves)
            where T : EntitySaveData
            where U : FactoryEntity
        {
            foreach (var entitySave in entitiesSaves)
            {
                var entity = FactoryMap.Instance.InstantiateEntity<U>(entitySave.id);
                entity.SetLayer(LayerType.Environment);
                entity.transform.SetAngleY(entitySave.rotationY);
                Place(entity, entitySave.gridPos);
                entity.SetSaveData(entitySave);
            }
        }

        public void PlaceOnCenter(Entity entity, Vector3 worldPos)
        {
            entity.SnapToGridOnCenter(worldPos);
            SetEntity(entity);
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
            SetEntity(entity);
        }

        public void Rotate(Entity entity, int rotAngleY)
        {
            entity.Rotate(rotAngleY);
            SetEntity(entity);
        }

        public void MakeConveyorsConnexions(Conveyor from, Conveyor to, Action<Conveyor, Conveyor> onConveyorReplaced)
        {
            var fromPrev = from.PrevConveyor;
            if (fromPrev != null && FactoryUtils.AreDiagonals(fromPrev.GridPos, to.GridPos))
            {
                var newFrom = ReplaceWithConveyorCorner(from, to, fromPrev.GridPos);
                fromPrev.Connect(newFrom);
                onConveyorReplaced?.Invoke(from, newFrom);
                from = newFrom;
            }
            else if (FactoryMap.Instance.IsDiagonalWithPossibleExtractorConnexion(from, to, out var fromPrevGridPos))
            {
                var newFrom = ReplaceWithConveyorCorner(from, to, fromPrevGridPos);
                onConveyorReplaced?.Invoke(from, newFrom);
                from = newFrom;
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

        public void ReplaceConveyorEndWithCornerForFeedTarget(Conveyor last, Vector2Int fromPrevGridPos,
            Vector2Int outDir, Action<Conveyor, Conveyor> onConveyorReplaced)
        {
            var prev = last.PrevConveyor;
            if (prev == null)
                return;

            var inDir = last.GridPos - fromPrevGridPos;
            var outDirForCorner = new Vector2Int(-outDir.x, -outDir.y);
            var corner = CreateCornerReplacement(last);
            corner.transform.SetLocalAngleY(ConveyorHelper.GetCornerAngle(inDir, outDirForCorner, out var speedSign));
            corner.SetBeltDirection(speedSign);
            corner.SnapToGrid(corner.GridPos);

            prev.Connect(corner);
            onConveyorReplaced?.Invoke(last, corner);
            corner.OnConfirmPlacement();
        }

        private Conveyor ReplaceWithConveyorCorner(Conveyor from, Conveyor to, Vector2Int fromPrevGridPos)
        {
            var inDir = from.GridPos - fromPrevGridPos;
            var outDir = to.GridPos - from.GridPos;

            var newFromConveyor = CreateCornerReplacement(from);
            newFromConveyor.transform.SetLocalAngleY(ConveyorHelper.GetCornerAngle(inDir, outDir, out var speedSign));
            newFromConveyor.SetBeltDirection(speedSign);

            to.transform.SetLocalAngleY(ConveyorHelper.GetStraightAngle(to.GridPos - newFromConveyor.GridPos));

            return newFromConveyor;
        }

        private static ConveyorCorner CreateCornerReplacement(Conveyor from)
        {
            var newFromConveyor = FactoryMap.Instance.InstantiateEntity<ConveyorCorner>(FactoryConstants.CONVEYOR_CORNER_NAME);
            Replace(from, newFromConveyor);
            newFromConveyor.ReleaseHighlightObject();
            return newFromConveyor;
        }

        private static void SetEntity(Entity entity)
        {
            FactoryMap.Instance.Remove(entity);

            Vector2Int right = entity.Right;
            Vector2Int forward = entity.Forward;
            Vector2Int origin = entity.GridPos;

            for (int x = 0; x < entity.Size.x; x++)
            {
                for (int y = 0; y < entity.Size.y; y++)
                {
                    Vector2Int gridPos = origin + right * x + forward * y;
                    if (!FactoryMap.Instance.HasEntity(gridPos))
                        FactoryMap.Instance.Add(entity, gridPos);
                }
            }

            entity.OnPlacementUpdate();

            FactoryMap.Instance.UpdateDebugCells();
        }

        private static void Replace(Conveyor replacedConveyor, Conveyor replacementConveyor)
        {
            Release(replacedConveyor);
            replacementConveyor.SetLayer(LayerType.Interactable);
            replacementConveyor.SnapToGrid(replacedConveyor.GridPos);
            SetEntity(replacementConveyor);
        }

        public static void Release(FactoryEntity entity)
        {
            FactoryMap.Instance.Remove(entity);
            FactoryMap.Instance.UpdateDebugCells();
            ObjectPoolingSystem.Instance.ReleaseObject(entity);
        }
    }
}
