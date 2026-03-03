using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameStateBuildExtractor : GameStateBuild<GameStateBuildExtractor.Context, Extractor>
{
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        if (context.extractor == null)
        {
            if (TryPlaceEntity() == false)
                return;    
        }
        else
        {
            _entity = context.extractor;
            _entity.SetActiveInputsOutputs(true);
            _entity.ApplyCorrectPlacement(_entity.IsCorrectlyPlaced);
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
            _entity.OnConfirmPlacement();
        else 
            FactorySystem.Instance.Release(_entity);
        
        await _buildPanel.Close(true, cancellationToken);
    }
    
    public new class Context : GameStateBuild<Context, Extractor>.Context
    {
        public Extractor extractor;
    }
}
