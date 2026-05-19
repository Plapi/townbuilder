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

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            _entityPanel = UISystem.Instance.GetPanel<UIEntityPanel>();
            _entityPanel.Init(new UIEntityPanel.Data
            {
                entity = context.entity
            });
            await _entityPanel.Show(cancellationToken);

            _entityPanel.ResetSelectedButton();
            await UniTask.WaitUntil(() => _entityPanel.SelectedButton != null, cancellationToken: cancellationToken);

            if (_entityPanel.SelectedButton == UIButtonType.StartConstruction && context.entity is Construction construction)
                construction.StartConstruction();
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