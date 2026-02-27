using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
{
    private Vector2Int _adjacentGridPos;
    private List<Conveyor> _conveyors;
    
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
        
        ActivateAllowedOutputs(_entity);
        
        _conveyors = new List<Conveyor>() { _entity };
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitingForDragStartFromAdjacentGridPos, ProcessDraggingFromAdjacentGridPos },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButtonPreview }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        _conveyors[^1].SetActiveInputsOutputs(false);
        await _buildPanel.Close(true, cancellationToken);
    }
    
    private async UniTask WaitingForDragStartFromAdjacentGridPos(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && Utils.MouseIsOverUI() == false && 
                                      TryGetAdjacentGridPos(_conveyors[^1], out _adjacentGridPos), cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessDraggingFromAdjacentGridPos(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        
        _conveyors[^1].SetActiveInputsOutputs(false);
        _conveyors.Add(CreateNewConveyor(_conveyors[^1], _adjacentGridPos));
        
        while (cancellationToken.IsCancellationRequested == false && Input.GetMouseButton(0))
        {
            await UniTask.NextFrame(cancellationToken: cancellationToken);
            
            if (TryGetAdjacentGridPos(_conveyors[^1], out var gridPos) && _adjacentGridPos != gridPos)
            {
                _conveyors[^1].SetActiveInputsOutputs(false);
                _conveyors.Add(CreateNewConveyor(_conveyors[^1], gridPos));
                _adjacentGridPos = gridPos;    
            }
            else
            if (_conveyors.Count > 1 && Layers.Raycast(_camera, Layers.InteractableLayer, out var hit))
            {
                var hitConveyor = hit.transform.GetComponent<EntityCollider>()?.Entity;
                if (hitConveyor == _conveyors[^1])
                    continue;
                
                for (int i = 0; i < _conveyors.Count; i++)
                {
                    if (hitConveyor != _conveyors[i])
                        continue;
                    
                    while (_conveyors.Count > 1 && _conveyors.Count > i + 1)
                    {
                        _conveyors[^2].Disconnect(_conveyors[^1]);
                        _conveyors[^1].SetActiveInputsOutputs(false);
                        FactorySystem.Instance.Release(_conveyors[^1]);
                        ObjectPoolingSystem.Instance.ReleaseObject(_conveyors[^1]);
                        _conveyors.RemoveAt(_conveyors.Count - 1);
                        
                        if (_conveyors[^1] is ConveyorCorner conveyorCorner)
                        {
                            Debug.LogError("ConveyorCorner");
                        }
                    }
                    ActivateAllowedOutputs(_conveyors[^1]);
                    break;
                }
            }
        }
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
    
    private static void ActivateAllowedOutputs(Conveyor conveyor)
    {
        foreach (var output in conveyor.Outputs)
        {
            var gridPos = FactoryUtils.GetGridPos(output);
            output.gameObject.SetActive(FactorySystem.Instance.HasEntity(gridPos) == false);
        }
    }
    
    private Conveyor CreateNewConveyor(Conveyor from, Vector2Int gridPos)
    {
        var newConveyor = InstantiateEntity();
        FactorySystem.Instance.Place(newConveyor, gridPos);
        FactorySystem.Instance.MakeConveyorsConnexions(from, newConveyor);
        ActivateAllowedOutputs(newConveyor);
        return newConveyor;
    }

    private bool TryGetAdjacentGridPos(Conveyor conveyor, out Vector2Int gridPos)
    {
        if (Layers.Raycast(_camera, Layers.GroundLayer, out var hit))
        {
            gridPos = FactoryUtils.WorldToGrid(hit.point, RoundType.Floor);
            return FactorySystem.Instance.HasEntity(gridPos) == false && FactoryUtils.AreAdjacent(conveyor.GridPos, gridPos);
        }
        gridPos = Vector2Int.zero;
        return false;
    }
    
    public new class Context : GameStateBuild<Context, Conveyor>.Context
    {
        
    }
}
