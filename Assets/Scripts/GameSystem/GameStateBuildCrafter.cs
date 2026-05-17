using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.Game;
using com.Plapamaru.TownCrafter.Layers;
using com.Plapamaru.TownCrafter.UI;
using Cysharp.Threading.Tasks;

public class GameStateBuildCrafter : GameStateBuild<GameStateBuildCrafter.Context, Crafter>
{
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        await base.Run(cancellationToken);

        if (context.crafter == null)
        {
            if (TryPlaceEntity() == false)
                return;
        }
        else
        {
            _entity = context.crafter;
            _entity.SetActiveInputsOutputs(true);
            _entity.ShowHighlightObject();
            _entity.SetLayer(LayerType.Interactable);
        }

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

        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            _entity.OnConfirmPlacement();
            ConnectInputConveyorsToCrafter();
        }
        else
            FactorySystem.Release(_entity);
    }

    private void ConnectInputConveyorsToCrafter()
    {
        foreach (var input in _entity.Inputs)
        {
            var inputGridPos = FactoryUtils.GetGridPos(input);
            if (FactoryMap.Instance.TryGetEntity(inputGridPos, out Conveyor conveyor) && conveyor.NextConveyor == null)
                conveyor.ConnectFeedTarget(_entity);
        }
    }

    public new class Context : GameStateBuild<Context, Crafter>.Context
    {
        public Crafter crafter;
    }
}