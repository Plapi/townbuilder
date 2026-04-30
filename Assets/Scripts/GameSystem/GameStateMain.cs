using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using com.Plapamaru.Utilities;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public class GameStateMain : GameState<GameStateMain.Context>
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private MobileTouchCamera _mobileTouchCamera;

        private UIMainPanel _mainPanel;

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            InitUI();

            await _mainPanel.Show(cancellationToken);

            await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
            {
                { ct => GameStateUtils.WaitingForTap(_mobileTouchCamera, ct), ProcessTap },
                { WaitForSelectedButton, ProcessSelectedButton },
                { WaitForCameraMovement, ProcessCameraMovement }
            }, cancellationToken);
        }

        public override UniTask Exit(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        private void InitUI()
        {
            _mainPanel = UISystem.Instance.GetPanel<UIMainPanel>();
            _mainPanel.Init(new UIMainPanel.Data());
            UISystem.Instance.GetPanel<UIBuildPanel>().Init(new UIBuildPanel.Data());
        }

        private async UniTask ProcessTap(CancellationToken cancellationToken)
        {
            if (Utils.MouseIsOverUI())
                return;

            if (FactoryUtils.TryGetMouseGridPosition(_camera, out var gridPos) && FactoryMap.Instance.TryGetEntity(gridPos, out FactoryEntity entity))
            {
                if (entity is Extractor extractor)
                {
                    await _mainPanel.Close(true, cancellationToken: cancellationToken);
                    EnqueueGameStateBuildExtractor(extractor);
                }
                else if (entity is Conveyor conveyor)
                {
                    await _mainPanel.Close(true, cancellationToken: cancellationToken);
                    EnqueueGameStateBuildConveyor(conveyor);
                }
            }
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

        private async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
        {
            _mainPanel.ResetSelectedButton();
            await UniTask.WaitUntil(() => _mainPanel.SelectedButton != null, cancellationToken: cancellationToken);
        }

        private async UniTask ProcessSelectedButton(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);

            if (_mainPanel.SelectedButton == UIButtonType.Extractor)
            {
                await _mainPanel.Close(true, cancellationToken: cancellationToken);
                EnqueueGameStateBuildExtractor(null);
            }
            else if (_mainPanel.SelectedButton == UIButtonType.Conveyor)
            {
                await _mainPanel.Close(true, cancellationToken: cancellationToken);
                EnqueueGameStateBuildConveyor(null);
            }
            else if (_mainPanel.SelectedButton == UIButtonType.Crafting)
            {

            }
        }

        private static void EnqueueGameStateBuildExtractor(Extractor extractor)
        {
            var context = new GameStateBuildExtractor.Context()
            {
                id = FactoryConstants.EXTRACTOR_NAME,
                extractor = extractor
            };
            GameSystem.Instance.EnqueueState<GameStateBuildExtractor, GameStateBuildExtractor.Context>(context, false);
        }

        private static void EnqueueGameStateBuildConveyor(Conveyor conveyor)
        {
            var context = new GameStateBuildConveyor.Context()
            {
                id = FactoryConstants.CONVEYOR_STRAIGHT_NAME,
                conveyor = conveyor
            };
            GameSystem.Instance.EnqueueState<GameStateBuildConveyor, GameStateBuildConveyor.Context>(context, false);
        }

        public new class Context : GameStateBase.Context
        {

        }
    }
}