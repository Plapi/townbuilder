using System;
using System.Threading;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public class GameStateEntityInfo : GameState<GameStateEntityInfo.Context>
    {
        private UIEntityPanel _entityPanel;

        private Func<bool> _shouldExitEntityInfo;

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            _entityPanel = UISystem.Instance.GetPanel<UIEntityPanel>();
            _entityPanel.Init(new UIEntityPanel.Data
            {
                entity = context.entity
            });
            await _entityPanel.Show(cancellationToken);

            SetShouldExitEntityInfo();

            _entityPanel.ResetSelectedButton();
            await UniTask.WaitUntil(ShouldExitEntityInfo, cancellationToken: cancellationToken);

            if (_entityPanel.SelectedButton == UIButtonType.StartConstruction && context.entity is Construction construction)
                construction.StartConstruction();
        }

        private void SetShouldExitEntityInfo()
        {
            _shouldExitEntityInfo = () => false;
            if (context.entity is Construction construction && construction.State != ConstructionState.Finished)
                _shouldExitEntityInfo = () => construction.State == ConstructionState.Finished;
        }

        private bool ShouldExitEntityInfo()
        {
            return _entityPanel.SelectedButton != null || _shouldExitEntityInfo();
        }

        public override async UniTask Exit(CancellationToken cancellationToken)
        {
            await _entityPanel.Close(true, cancellationToken);
            SimulationClock.SetPaused(false);
        }

        public new class Context : GameStateBase.Context
        {
            public Entity entity;
        }
    }
}