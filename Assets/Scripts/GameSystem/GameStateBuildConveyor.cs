using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Utilities;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.Layers;
using com.Plapamaru.TownCrafter.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
    {
        [Header("Runtime Properties")]
        [SerializeField] private List<Conveyor> _conveyors;

        private BuildStep _nextBuildStep;

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            await base.Run(cancellationToken);

            if (context.conveyor == null)
            {
                if (TryPlaceEntity() == false)
                    return;
                _entity.SetPillarActive(true);
            }
            else
            {
                _entity = context.conveyor;
                _entity.Disconnect();
                _entity.ShowHighlightObject();
                _entity.SetLayer(LayerType.Interactable);
                _buildPanel.UpdateCancelButton(false);
            }

            _entity.SetActiveInputsOutputs(false);
            SetActivatePossibleConnexions(true);

            await _buildPanel.Show(cancellationToken);

            await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
            {
                { ct => GameStateUtils.WaitingForTap(_mobileTouchCamera, ct), ProcessTap },
                { WaitForTouchOnEntity, ProcessEntityPlacement },
                { WaitForCameraMovement, ProcessCameraMovement },
                { WaitForSelectedButton, ProcessSelectedButton }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (_buildPanel.SelectedButton == UIButtonType.Close)
            {
                FactorySystem.Release(_entity);
                return;
            }

            if (_entity is ConveyorCorner)
            {
                var gridPos = _entity.GridPos;
                FactorySystem.Release(_entity);
                _entity = InstantiateEntity();
                _factorySystem.Place(_entity, gridPos);
            }

            _entity.TryShowAllowedHighlights();
            _conveyors.Add(_entity);

            if (_entity.TryGetAjdConveyor(c => c.NextConveyor == null, out Conveyor firstConveyor))
            {
                _conveyors.Insert(0, firstConveyor);
                _factorySystem.MakeConveyorsConnexions(firstConveyor, _entity, OnConveyorReplaced);
                _entity = firstConveyor;
            }

            _buildPanel.SetRotateButtonsInteractable(false);

            await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
            {
                { WaitingForDragStart, ProcessDragging },
                { WaitForCameraMovement, ProcessCameraMovement },
                { WaitForSelectedButton, ProcessSelectedButton1 }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            foreach (var conveyor in _conveyors)
                conveyor.OnConfirmPlacement();

            if (_buildPanel.SelectedButton == UIButtonType.Close)
            {
                foreach (var conveyor in _conveyors)
                    FactorySystem.Release(conveyor);
            }

            if (_buildPanel.SelectedButton != UIButtonType.Close)
            {
                if (TryGetConstructionFromInput(out var construction, out var matchedInput))
                {
                    var lastConveyor = _conveyors[^1];
                    if (ShouldReplaceLastWithCornerForConstruction(lastConveyor, matchedInput, out var outDir, out var fromPrevGridPos))
                        _factorySystem.ReplaceConveyorEndWithCornerForConstruction(lastConveyor, fromPrevGridPos, outDir, OnConveyorReplaced);
                    _conveyors[^1].ConnectConstruction(construction);
                }
                else if (TryGetAdjLastConveyor(out var lastConveyor))
                {
                    _factorySystem.MakeConveyorsConnexions(_conveyors[^1], lastConveyor, (_, newConveyor) =>
                    {
                        newConveyor.SetLayer(LayerType.Environment);
                    });
                }
            }
        }

        private static bool ShouldReplaceLastWithCornerForConstruction(Conveyor last, Transform matchedInput,
            out Vector2Int outDir, out Vector2Int fromPrevGridPos)
        {
            outDir = default;
            fromPrevGridPos = default;
            if (last is ConveyorCorner)
                return false;

            var prev = last.PrevConveyor;
            if (prev == null)
                return false;

            fromPrevGridPos = prev.GridPos;
            var inDir = new Vector2Int(
                Mathf.Clamp(last.GridPos.x - prev.GridPos.x, -1, 1),
                Mathf.Clamp(last.GridPos.y - prev.GridPos.y, -1, 1));
            if (Mathf.Abs(inDir.x) + Mathf.Abs(inDir.y) != 1)
                return false;

            if (FactoryUtils.TryGetConstructionFeedOutDir(matchedInput, out outDir) == false)
                return false;

            if (inDir.x * outDir.x + inDir.y * outDir.y != 0)
                return false;

            return true;
        }

        private bool TryGetConstructionFromInput(out Construction construction, out Transform matchedInput)
        {
            construction = null;
            matchedInput = null;
            return _conveyors.Count > 1 &&
                   FactoryMap.Instance.TryGetConstructionFromInput(_conveyors[^1].GridPos, out construction, out matchedInput);
        }

        private bool TryGetAdjLastConveyor(out Conveyor lastConveyor)
        {
            lastConveyor = null;
            return _conveyors.Count > 0 &&
                   _conveyors[^1].TryGetAjdConveyor(c => c.PrevConveyor == null, out lastConveyor) &&
                   _conveyors[^1].PrevConveyor != lastConveyor;
        }

        public override async UniTask Exit(CancellationToken cancellationToken)
        {
            await base.Exit(cancellationToken);

            _conveyors.Clear();
            _buildPanel.SetRotateButtonsInteractable(true);
            _buildPanel.UpdateCancelButton(true);
            SetActivatePossibleConnexions(false);
        }

        private async UniTask WaitingForDragStart(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) &&
                                          Utils.MouseIsOverUI() == false &&
                                          UpdateNextBuildingStep(true),
                cancellationToken: cancellationToken);
        }

        private async UniTask ProcessDragging(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (_nextBuildStep != null)
                    ProcessNextBuildStep();

                await UniTask.NextFrame(cancellationToken: cancellationToken);

                if (Input.GetMouseButton(0) == false)
                    return;

                UpdateNextBuildingStep(false);
            }
        }

        private UniTask ProcessSelectedButton1(CancellationToken cancellationToken)
        {
            if (_buildPanel.SelectedButton == UIButtonType.Confirm || _buildPanel.SelectedButton == UIButtonType.Close)
                ExitBaseRun = true;
            return UniTask.CompletedTask;
        }

        private void ProcessNextBuildStep()
        {
            if (_nextBuildStep.build)
            {
                if (FactoryMap.Instance.TryFindPath(_conveyors[^1], _nextBuildStep.gridPos, out var path) == false)
                    return;

                _conveyors[^1].ReleaseAllowedHighlights();
                _conveyors[^1].ReleaseHighlightObject();

                foreach (var gridPos in path)
                    _conveyors.Add(CreateNewConveyor(_conveyors[^1], gridPos));
            }
            else
            {
                var index = _conveyors.IndexOf(_nextBuildStep.removeConveyor);
                if (index == -1)
                    return;

                while (_conveyors.Count > 1 && _conveyors.Count > index + 1)
                    RemoveLastConveyor();

                if (_conveyors[^1] is ConveyorCorner)
                {
                    var lastConveyorGridPos = _conveyors[^1].GridPos;
                    RemoveLastConveyor();
                    if (_conveyors.Count > 0)
                        _conveyors.Add(CreateNewConveyor(_conveyors[^1], lastConveyorGridPos));
                    else
                    {
                        _entity = InstantiateEntity();
                        _factorySystem.Place(_entity, lastConveyorGridPos);
                        _conveyors.Add(_entity);
                    }
                }
            }

            _conveyors[^1].TryShowAllowedHighlights();
            _conveyors[^1].ShowHighlightObject();
        }

        private bool UpdateNextBuildingStep(bool onlyAdjacent)
        {
            _nextBuildStep = null;

            if (LayersUtils.Raycast(_camera, LayerType.Ground, out var hit) == false)
                return false;

            var gridPos = FactoryUtils.WorldToGrid(hit.point, RoundType.Floor);

            if (FactoryMap.Instance.TryGetEntity(gridPos, out Conveyor removeConveyor))
            {
                _nextBuildStep = new BuildStep
                {
                    build = false,
                    gridPos = removeConveyor.GridPos,
                    removeConveyor = removeConveyor
                };
                return true;
            }

            var lastConveyor = _conveyors.Count > 0 ? _conveyors[^1] : null;
            if (lastConveyor == null)
                return false;

            if (onlyAdjacent && FactoryUtils.AreNeighbour(lastConveyor.GridPos, gridPos) == false)
                return false;

            _nextBuildStep = new BuildStep
            {
                build = true,
                gridPos = gridPos,
                removeConveyor = null
            };

            return true;
        }

        private void RemoveLastConveyor()
        {
            if (_conveyors.Count > 1)
                _conveyors[^2].Disconnect(_conveyors[^1]);

            if (_conveyors.Count == 0)
            {
                Debug.LogError("Remove Failed");
                return;
            }

            FactorySystem.Release(_conveyors[^1]);
            _conveyors.RemoveAt(_conveyors.Count - 1);
        }

        private Conveyor CreateNewConveyor(Conveyor from, Vector2Int gridPos)
        {
            var newConveyor = InstantiateEntity();
            _factorySystem.Place(newConveyor, gridPos);
            _factorySystem.MakeConveyorsConnexions(from, newConveyor, OnConveyorReplaced);
            return newConveyor;
        }

        private void OnConveyorReplaced(Conveyor oldConveyor, Conveyor newConveyor)
        {
            for (int i = 0; i < _conveyors.Count; i++)
                if (_conveyors[i] == oldConveyor)
                    _conveyors[i] = newConveyor;
        }

        private void SetActivatePossibleConnexions(bool active)
        {
            FactoryMap.Instance.SetActiveInputsOutputs(
                (false, active, typeof(Extractor)),
                (true, active, typeof(Construction)));
        }

        public new class Context : GameStateBuild<Context, Conveyor>.Context
        {
            public Conveyor conveyor;
        }

        private class BuildStep
        {
            public bool build;
            public Vector2Int gridPos;
            public Conveyor removeConveyor;
        }
    }
}