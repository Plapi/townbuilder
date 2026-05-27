using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.Layers;
using com.Plapamaru.TownCrafter.UI;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public class GameStateDelete : GameState<GameStateDelete.Context>
    {
        private static readonly HashSet<Type> _ignoredDeleteTypes = new HashSet<Type>()
        {
            typeof(ResourceNode),
            typeof(Construction)
        };

        [SerializeField] private Camera _camera;
        [SerializeField] private MobileTouchCamera _mobileTouchCamera;
        [SerializeField] private FactorySystem _factorySystem;
        [SerializeField] private MultipleHighlightSelection _highlightSelection;

        private UIDeletePanel _deletePanel;

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            SimulationClock.SetPaused(true);
            GetFactorySystem()?.FadeInGrid();

            _deletePanel = UISystem.Instance.GetPanel<UIDeletePanel>();
            await _deletePanel.Show(cancellationToken);

            await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
            {
                { ct => GameStateUtils.WaitingForTap(_mobileTouchCamera, ct), ProcessTap },
                { WaitForSelectedButton, ProcessSelectedButton },
                { WaitForCameraMovement, ProcessCameraMovement },
                { ct => GameStateUtils.WaitForDrag(_mobileTouchCamera, ct), ProcessDragging }
            }, cancellationToken);
        }

        public override async UniTask Exit(CancellationToken cancellationToken)
        {
            await _deletePanel.Close(true, cancellationToken);
            GetFactorySystem()?.FadeOutGrid();
            SimulationClock.SetPaused(false);
        }

        private FactorySystem GetFactorySystem()
        {
            if (_factorySystem == null)
                _factorySystem = UnityEngine.Object.FindFirstObjectByType<FactorySystem>();

            return _factorySystem;
        }

        private UniTask ProcessTap(CancellationToken cancellationToken)
        {
            if (!Utils.MouseIsOverUI() &&
                LayersUtils.Raycast(_camera, LayerType.Environment, out var hit) &&
                hit.transform.TryGetComponent(out EntityCollider entityCollider) &&
                entityCollider.Entity != null && entityCollider.Entity is FactoryEntity factoryEntity &&
                !_ignoredDeleteTypes.Contains(factoryEntity.GetType()))
            {
                FactorySystem.Release(factoryEntity);
            }

            return UniTask.CompletedTask;
        }

        private async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
        {
            _deletePanel.ResetSelectedButton();
            await UniTask.WaitUntil(() => _deletePanel.SelectedButton != null, cancellationToken: cancellationToken);
        }

        private UniTask ProcessSelectedButton(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);
            if (_deletePanel.SelectedButton == UIButtonType.Close)
                ExitBaseRun = true;
            return UniTask.CompletedTask;
        }

        private async UniTask WaitForCameraMovement(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(true);
            await UniTask.WaitUntil(() => _mobileTouchCamera.HasInteraction, cancellationToken: cancellationToken);
        }

        private async UniTask ProcessCameraMovement(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => _mobileTouchCamera.HasInteraction == false, cancellationToken: cancellationToken);
        }

        private async UniTask ProcessDragging(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);
            var entities = new HashSet<FactoryEntity>();
            _highlightSelection.gameObject.SetActive(true);

            FactoryUtils.TryGetMouseGridPosition(_camera, out var fromGridPos);
            var toGridPos = fromGridPos;
            _highlightSelection.SetFromGridRange(fromGridPos, fromGridPos);

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (FactoryUtils.TryGetMouseGridPosition(_camera, out var gridPos) && toGridPos != gridPos)
                {
                    toGridPos = gridPos;
                    _highlightSelection.SetFromGridRange(fromGridPos, toGridPos);

                    foreach (var entity in entities)
                        entity.ReleaseHighlightObject();
                    FactoryMap.Instance.GetFactoryEntitiesInGridRange(fromGridPos, toGridPos, entities, _ignoredDeleteTypes);
                    foreach (var entity in entities)
                    {
                        entity.ShowHighlightObject();
                        entity.SetHighlightObjectColor(FactoryConfig.Instance.wrongColor);
                    }
                }

                await UniTask.NextFrame(cancellationToken: cancellationToken);

                if (Input.GetMouseButton(0) == false)
                    break;
            }

            _highlightSelection.gameObject.SetActive(false);

            foreach (var entity in entities)
            {
                entity.ReleaseHighlightObject();
                FactorySystem.Release(entity);
            }
        }

        public new class Context : GameStateBase.Context
        {

        }
    }
}
