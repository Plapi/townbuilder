using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
{
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        if (TryPlaceEntity() == false)
            return;
        _entity.SetActiveInputsOutputs(false);
        
        await _buildPanel.Show(cancellationToken);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnEntity, ProcessEntityPlacement },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButton }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        if (_confirmEntityPlacement == false)
        {
            FactorySystem.Instance.Release(_entity);
            ObjectPoolingSystem.Instance.ReleaseObject(_entity);
            await _buildPanel.Close(true, cancellationToken);
            return;
        }
        
        FactorySystem.Instance.ConfirmPlacement(_entity);
        ActivateAllowedOutputs();
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitingForTap, ProcessTap },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButtonPreview }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        _entity.SetActiveInputsOutputs(false);
        await _buildPanel.Close(true, cancellationToken);
    }
    
    private UniTask ProcessTap(CancellationToken cancellationToken)
    {
        if (Layers.Raycast(_camera, Layers.GroundLayer, out var hit) == false)
            return UniTask.CompletedTask;
        
        var gridPos = FactoryUtils.WorldToGrid(hit.point, RoundType.Floor);
        if (FactorySystem.Instance.HasEntity(gridPos) == false && FactoryUtils.AreAdjacent(_entity.GridPos, gridPos))
        {
            var newConveyor = InstantiateEntity();
            FactorySystem.Instance.Place(newConveyor, gridPos);
            FactorySystem.Instance.ConfirmPlacement(newConveyor);
            
            _entity.SetActiveInputsOutputs(false);
            
            FactorySystem.Instance.MakeConveyorsConnexions(_entity, newConveyor);
            
            _entity = newConveyor;
            
            ActivateAllowedOutputs();
        }
        
        return UniTask.CompletedTask;
    }
    
    private UniTask ProcessSelectedButtonPreview(CancellationToken cancellationToken)
    {
        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            ExitBaseRun = true;
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Rotate)
        {
            
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            
        }
        
        return UniTask.CompletedTask;
    }
    
    private void ActivateAllowedOutputs()
    {
        foreach (var output in _entity.Outputs)
        {
            var gridPos = FactoryUtils.GetGridPos(output);
            output.gameObject.SetActive(FactorySystem.Instance.HasEntity(gridPos) == false);
        }
    }
    
    public new class Context : GameStateBuild<Context, Conveyor>.Context
    {
        
    }
}
