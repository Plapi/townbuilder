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
        _entity.SetPillarActive(true);
        
        await _buildPanel.Show(cancellationToken);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnEntity, ProcessEntityPlacement },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButton }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            FactorySystem.Instance.Release(_entity);
            await _buildPanel.Close(true, cancellationToken);
            return;
        }
        
        _entity.ReleaseHighlightObject();
        _entity.ShowAllowedHighlights();
        
        _conveyors.Add(_entity);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitingForDragStart, ProcessDragging },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButton }
        }, cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return;
        
        foreach (var conveyor in _conveyors)
            conveyor.OnConfirmPlacement();
        
        if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            foreach (var conveyor in _conveyors)
                FactorySystem.Instance.Release(conveyor);
        }
        
        _conveyors.Clear();
        
        await _buildPanel.Close(true, cancellationToken);
    }
    
    private async UniTask WaitingForDragStart(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && 
                                      Utils.MouseIsOverUI() == false && 
                                      UpdateNextBuildingStep(true), 
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
            
            UpdateNextBuildingStep(false);
        }
    }
    
    protected override UniTask ProcessSelectedButton(CancellationToken cancellationToken)
    {
        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            ExitBaseRun = true;
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            ExitBaseRun = true;
        }
        
        return UniTask.CompletedTask;
    }
    
    private void ProcessNextBuildStep()
    {
        if (_nextBuildStep.build)
        {
            if (FactorySystem.Instance.TryFindPath(_conveyors[^1], _nextBuildStep.gridPos, out var path) == false)
                return;
            
            _conveyors[^1].ReleaseAllowedHighlights();
            
            foreach (var gridPos in path)
                _conveyors.Add(CreateNewConveyor(_conveyors[^1], gridPos));    
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
        }
        
        _conveyors[^1].ShowAllowedHighlights();
    }
    
    private bool UpdateNextBuildingStep(bool onlyAdjacent)
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
        
        if (onlyAdjacent && FactoryUtils.AreNeighbour(lastConveyor.GridPos, gridPos) == false)
            return false;
        
        _nextBuildStep = new BuildStep
        {
            build = true,
            gridPos = gridPos,
            removeConveyor = null
        };
        
        return true;
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
        
        FactorySystem.Instance.Release(_conveyors[^1]);
        _conveyors.RemoveAt(_conveyors.Count - 1);
    }
    
    private Conveyor CreateNewConveyor(Conveyor from, Vector2Int gridPos)
    {
        var newConveyor = InstantiateEntity();
        FactorySystem.Instance.Place(newConveyor, gridPos);
        FactorySystem.Instance.MakeConveyorsConnexions(from, newConveyor, OnConveyorReplaced);
        newConveyor.ReleaseHighlightObject();
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
