using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
{
    [Header("Runtime Properties")]
    [SerializeField] private List<Conveyor> _conveyors;
    
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
            await _buildPanel.Close(true, cancellationToken);
            return;
        }
        
        ActivateAllowedOutputs(_entity);
        
        _conveyors = new List<Conveyor>() { _entity };
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitingForDragStart, ProcessDragging },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButtonPreview }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        _conveyors[^1].SetActiveInputsOutputs(false);
        await _buildPanel.Close(true, cancellationToken);
    }
    
    private async UniTask WaitingForDragStart(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && Utils.MouseIsOverUI() == false && 
                                      (TryGetAdjacentGridPos(_conveyors[^1], out _) || 
                                       _conveyors.Count > 1 && Layers.Raycast(_camera, Layers.InteractableLayer, out _)), 
            cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessDragging(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        var adjacentGridPos = (Vector2Int?)null;
        
        while (cancellationToken.IsCancellationRequested == false && Input.GetMouseButton(0))
        {
            await UniTask.NextFrame(cancellationToken: cancellationToken);
            
            if (TryGetAdjacentGridPos(_conveyors[^1], out var gridPos) && adjacentGridPos != gridPos)
            {
                _conveyors[^1].SetActiveInputsOutputs(false);
                _conveyors.Add(CreateNewConveyor(_conveyors[^1], gridPos));
                adjacentGridPos = gridPos;  
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
                        RemoveLastConveyor();
                    
                    if (_conveyors[^1] is ConveyorCorner)
                    {
                        var lastConveyorGridPos = _conveyors[^1].GridPos;
                        RemoveLastConveyor();
                        if (_conveyors.Count > 0)
                            _conveyors.Add(CreateNewConveyor(_conveyors[^1], lastConveyorGridPos));
                    }
                    
                    ActivateAllowedOutputs(_conveyors[^1]);
                    adjacentGridPos = null;
                    break;
                }
            }
        }
    }
    
    private void RemoveLastConveyor()
    {
        if (_conveyors.Count > 1)
            _conveyors[^2].Disconnect(_conveyors[^1]);

        if (_conveyors.Count == 0)
        {
            Debug.LogError("Remove Failed");
            return;
        }
        
        _conveyors[^1].SetActiveInputsOutputs(false);
        FactorySystem.Instance.Release(_conveyors[^1]);
        _conveyors.RemoveAt(_conveyors.Count - 1);
    }
    
    [ContextMenu("Test")]
    private void Test()
    {
        while (_conveyors.Count > 1)
            RemoveLastConveyor();
        ActivateAllowedOutputs(_conveyors[^1]);
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
        FactorySystem.Instance.MakeConveyorsConnexions(from, newConveyor, OnConveyorReplaced);
        ActivateAllowedOutputs(newConveyor);
        return newConveyor;
    }
    
    private void OnConveyorReplaced(Conveyor replacedConveyor, Conveyor replacementConveyor)
    {
        for (int i = 0; i < _conveyors.Count; i++)
            if (_conveyors[i] == replacedConveyor)
                _conveyors[i] = replacementConveyor;
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
