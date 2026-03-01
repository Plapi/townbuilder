using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
{
    [Header("Runtime Properties")]
    [SerializeField] private List<Conveyor> _conveyors;

    private BuildStep _nextBuildStep;
    
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
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && 
                                      Utils.MouseIsOverUI() == false && 
                                      UpdateNextBuildingStep(), 
            cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessDragging(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        
        while (cancellationToken.IsCancellationRequested == false)
        {
            if (_nextBuildStep != null)
                ProcessNextBuildStep();
            
            await UniTask.NextFrame(cancellationToken: cancellationToken);
            
            if (Input.GetMouseButton(0) == false)
                return;
            
            UpdateNextBuildingStep();
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
    
    [ContextMenu("Remove All Conveyors")]
    private void RemoveAllConveyors()
    {
        while (_conveyors.Count > 1)
            RemoveLastConveyor();
        ActivateAllowedOutputs(_conveyors[^1]);
    }
    
    private void ProcessNextBuildStep()
    {
        if (_nextBuildStep.build)
        {
            _conveyors[^1].SetActiveInputsOutputs(false);
            _conveyors.Add(CreateNewConveyor(_conveyors[^1], _nextBuildStep.gridPos));
        }
        else
        {
            var index = _conveyors.IndexOf(_nextBuildStep.removeConveyor);
            if (index == -1)
                return;
            
            while (_conveyors.Count > 1 && _conveyors.Count > index + 1)
                RemoveLastConveyor();
            
            if (_conveyors[^1] is ConveyorCorner)
            {
                var lastConveyorGridPos = _conveyors[^1].GridPos;
                RemoveLastConveyor();
                if (_conveyors.Count > 0)
                    _conveyors.Add(CreateNewConveyor(_conveyors[^1], lastConveyorGridPos));
            }
            
            ActivateAllowedOutputs(_conveyors[^1]);
        }
    }
    
    private bool UpdateNextBuildingStep()
    {
        _nextBuildStep = null;
        
        if (Layers.Raycast(_camera, Layers.GroundLayer, out var hit) == false)
            return false;
        
        var gridPos = FactoryUtils.WorldToGrid(hit.point, RoundType.Floor);
        if (FactorySystem.Instance.HasEntity(gridPos))
        {
            if (FactorySystem.Instance.TryGetEntity(gridPos, out Conveyor removeConveyor) && 
                _conveyors[^1] != removeConveyor && _conveyors.Contains(removeConveyor))
            {
                _nextBuildStep = new BuildStep
                {
                    build = false,
                    gridPos = removeConveyor.GridPos,
                    removeConveyor = removeConveyor
                };
                return true;
            }
            return false;
        }
        
        var lastConveyor = _conveyors.Count > 0 ? _conveyors[^1] : null;
        if (lastConveyor == null)
            return false;
        
        if (FactoryUtils.AreAdjacent(lastConveyor.GridPos, gridPos))
        {
            _nextBuildStep = new BuildStep
            {
                build = true,
                gridPos = gridPos,
                removeConveyor = null
            };
            return true;
        }
        
        return false;
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
    
    public new class Context : GameStateBuild<Context, Conveyor>.Context
    {
        
    }
    
    private class BuildStep
    {
        public bool build;
        public Vector2Int gridPos;
        public Conveyor removeConveyor;
    }
}
