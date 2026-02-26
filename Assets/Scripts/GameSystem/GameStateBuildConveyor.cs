using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateBuildConveyor : GameStateBuild<GameStateBuildConveyor.Context, Conveyor>
{
    private List<Conveyor> _previewConveyors;
    
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
        
        _entity.SetActiveInputsOutputs(false);
        
        if (_confirmEntityPlacement == false)
        {
            FactorySystem.Instance.Release(_entity);
            ObjectPoolingSystem.Instance.ReleaseObject(_entity);
            await _buildPanel.Close(true, cancellationToken);
            return;
        }
        
        FactorySystem.Instance.ConfirmPlacement(_entity);
        
        _previewConveyors = PlacePreviewConveyors();
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnEntity, ProcessConveyorPlacementPreview },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButtonPreview }
        }, cancellationToken);
    }

    private async UniTask ProcessConveyorPlacementPreview(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);

        while (Input.GetMouseButton(0) && cancellationToken.IsCancellationRequested == false)
        {
            await UniTask.NextFrame(cancellationToken: cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested == false)
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Constants.RAY_MAX_DISTANCE, 1 << Constants.InteractableLayer) &&
                hit.transform.parent.TryGetComponent(out Conveyor hitConveyor))
            {
                foreach (var conveyor in _previewConveyors)
                {
                    if (conveyor == hitConveyor)
                    {
                        FactorySystem.Instance.ConfirmPlacement(conveyor);
                        _entity = conveyor;
                    }
                    else
                    {
                        FactorySystem.Instance.Release(conveyor);
                        ObjectPoolingSystem.Instance.ReleaseObject(conveyor);
                    }
                }
                _previewConveyors = PlacePreviewConveyors();
            }
        }
    }
    
    private UniTask ProcessSelectedButtonPreview(CancellationToken cancellationToken)
    {
        return UniTask.CompletedTask;
    }
    
    private List<Conveyor> PlacePreviewConveyors()
    {
        var conveyors = new List<Conveyor>();
        Vector2Int[] directions = { Vector2Int.up , Vector2Int.down, Vector2Int.right, Vector2Int.left };
        for (var i = 0; i < directions.Length; i++)
        {
            var direction = directions[i];
            var gridPos = _entity.GridPos + direction;
            if (FactorySystem.Instance.HasEntity(gridPos) == false)
            {
                var conveyor = InstantiateEntity();
                var angle = FactoryUtils.GetAngle((EntityDirection)i);
                conveyor.transform.SetLocalAngleY(angle);
                FactorySystem.Instance.Place(conveyor, gridPos);
                conveyors.Add(conveyor);
            }
        }
        return conveyors;
    }
    
    public new class Context : GameStateBuild<Context, Conveyor>.Context
    {
        
    }
}
