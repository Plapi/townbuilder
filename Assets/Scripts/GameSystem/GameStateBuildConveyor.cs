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
                _entity.ApplyCorrectPlacement(_entity.IsCorrectlyPlaced);
                _entity.SetLayer(LayerType.Interactable);
                _buildPanel.UpdateCancelButton(false);
            }

            _entity.SetActiveInputsOutputs(false);

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
                _factorySystem.Release(_entity);
                await _buildPanel.Close(true, cancellationToken);
                _buildPanel.UpdateCancelButton(true);
                return;
            }

            if (_entity is ConveyorCorner)
            {
                var gridPos = _entity.GridPos;
                _factorySystem.Release(_entity);
                _entity = InstantiateEntity();
                _factorySystem.Place(_entity, gridPos);
            }

            _entity.ReleaseHighlightObject();
            _entity.ShowAllowedHighlights(_factorySystem.HasEntity);
            _conveyors.Add(_entity);

            if (_entity.TryGetAjdConveyor(_factorySystem.GetEntity, c => c.NextConveyor == null, out Conveyor firstConveyor))
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
                    _factorySystem.Release(conveyor);
            }

            if (_conveyors.Count > 0 && _conveyors[^1].TryGetAjdConveyor(_factorySystem.GetEntity, c => c.PrevConveyor == null, out Conveyor lastConveyor) &&
                _conveyors[^1].PrevConveyor != lastConveyor)
                _factorySystem.MakeConveyorsConnexions(_conveyors[^1], lastConveyor, null);

            _conveyors.Clear();

            await _buildPanel.Close(true, cancellationToken);

            _buildPanel.SetRotateButtonsInteractable(true);
            _buildPanel.UpdateCancelButton(true);
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
                if (_factorySystem.TryFindPath(_conveyors[^1], _nextBuildStep.gridPos, out var path) == false)
                    return;

                _conveyors[^1].ReleaseAllowedHighlights();

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

                if (_conveyors[^1] is ConveyorCorner && _conveyors.Count > 1)
                {
                    var lastConveyorGridPos = _conveyors[^1].GridPos;
                    RemoveLastConveyor();
                    _conveyors.Add(CreateNewConveyor(_conveyors[^1], lastConveyorGridPos));
                }
            }

            _conveyors[^1].ShowAllowedHighlights(_factorySystem.HasEntity);
        }

        private bool UpdateNextBuildingStep(bool onlyAdjacent)
        {
            _nextBuildStep = null;

            if (LayersUtils.Raycast(_camera, LayerType.Ground, out var hit) == false)
                return false;

            var gridPos = FactoryUtils.WorldToGrid(hit.point, RoundType.Floor);
            if (_factorySystem.HasEntity(gridPos))
            {
                if (_factorySystem.TryGetEntity(gridPos, out Conveyor removeConveyor) &&
                    _conveyors[^1] != removeConveyor && _conveyors.Contains(removeConveyor))
                {
                    if (removeConveyor is ConveyorCorner && _conveyors.IndexOf(removeConveyor) == 0)
                        return false;

                    _nextBuildStep = new BuildStep
                    {
                        build = false,
                        gridPos = removeConveyor.GridPos,
                        removeConveyor = removeConveyor
                    };
                    return true;
                }
                return false;
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

            _factorySystem.Release(_conveyors[^1]);
            _conveyors.RemoveAt(_conveyors.Count - 1);
        }

        private Conveyor CreateNewConveyor(Conveyor from, Vector2Int gridPos)
        {
            var newConveyor = InstantiateEntity();
            _factorySystem.Place(newConveyor, gridPos);
            _factorySystem.MakeConveyorsConnexions(from, newConveyor, OnConveyorReplaced);
            newConveyor.ReleaseHighlightObject();
            return newConveyor;
        }

        private void OnConveyorReplaced(Conveyor replacedConveyor, Conveyor replacementConveyor)
        {
            for (int i = 0; i < _conveyors.Count; i++)
                if (_conveyors[i] == replacedConveyor)
                    _conveyors[i] = replacementConveyor;
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
