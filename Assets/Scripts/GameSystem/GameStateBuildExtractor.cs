using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameStateBuildExtractor : GameStateBuild<GameStateBuildExtractor.Context, Extractor>
{
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        if (TryPlaceEntity() == false)
            return;
        
        await _buildPanel.Show(cancellationToken);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnEntity, ProcessEntityPlacement },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButton }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        _entity.gameObject.SetLayerRecursively(0);
        _entity.SetActiveInputsOutputs(false);
        
        if (_confirmEntityPlacement)
        {
            FactorySystem.Instance.ConfirmPlacement(_entity);
        }
        else
        {
            FactorySystem.Instance.Release(_entity);
            ObjectPoolingSystem.Instance.ReleaseObject(_entity);
        }
        
        await _buildPanel.Close(true, cancellationToken);
    }
    
    public new class Context : GameStateBuild<Context, Extractor>.Context
    {
        
    }
}
