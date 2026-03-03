using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
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
        [SerializeField] private FactorySystem _factorySystem;

        private UIMainPanel _mainPanel;

        public new class Context : GameStateBase.Context
        {

        }

        private void Start()
        {
            InitUI();
            GameSystem.Instance.EnqueueState<GameStateMain, Context>(new Context(), true);
        }

        private void InitUI()
        {
            _mainPanel = UISystem.Instance.GetPanel<UIMainPanel>();
            _mainPanel.Init(new UIMainPanel.Data());
            UISystem.Instance.GetPanel<UIBuildPanel>().Init(new UIBuildPanel.Data());
        }

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            await _mainPanel.Show(cancellationToken);

            await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
            {
                { ct => GameStateUtils.WaitingForTap(_mobileTouchCamera, ct), ProcessTap },
                { WaitForSelectedButton, ProcessSelectedButton },
                { WaitForCameraMovement, ProcessCameraMovement }
            }, cancellationToken);
        }

        private async UniTask ProcessTap(CancellationToken cancellationToken)
        {
            if (FactoryUtils.TryGetMouseGridPosition(_camera, out var gridPos) && _factorySystem.TryGetEntity(gridPos, out FactoryEntity entity))
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
    }
}