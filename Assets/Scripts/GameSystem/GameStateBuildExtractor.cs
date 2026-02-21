using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateBuildExtractor : GameState<GameStateBuildExtractor.Context>
{
    private const float RAY_MAX_DISTANCE = 1000f;
    
    [SerializeField] private Camera _camera;
    [SerializeField] private MobileTouchCamera _mobileTouchCamera;
    [SerializeField] private Extractor _extractor;
    
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _factoryEntityLayer;
    
    public new class Context : GameStateBase.Context
    {
        
    }

    public override async UniTask Run(CancellationToken cancellationToken)
    {
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnExtractor, ProcessExtractorPlacement },
            { WaitForCameraMovement, ProcessCameraMovement }
        }, cancellationToken);
    }
    
    private async UniTask WaitForTouchOnExtractor(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() =>
        {
            if (Input.GetMouseButton(0) == false || Utils.MouseIsOverUI())
                return false;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out _, RAY_MAX_DISTANCE, _factoryEntityLayer);
        }, cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessExtractorPlacement(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = false;
        if (Utils.TryGetMouseWorldPosition(_camera, _groundLayer, RAY_MAX_DISTANCE, out Vector3 firstWorldPos) == false)
            return;
        var extractorFirstPos = _extractor.transform.position;
        
        while (Input.GetMouseButton(0) && cancellationToken.IsCancellationRequested == false)
        {
            if (Utils.TryGetMouseWorldPosition(_camera, _groundLayer, RAY_MAX_DISTANCE, out Vector3 worldPos))
            {
                var translation = worldPos - firstWorldPos;
                _extractor.Place(extractorFirstPos + translation); 
            }
            
            await UniTask.NextFrame(cancellationToken: cancellationToken);
        }
    }
    
    private async UniTask WaitForCameraMovement(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = true;
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging || _mobileTouchCamera.IsPinching, cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessCameraMovement(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging == false && _mobileTouchCamera.IsPinching == false, cancellationToken: cancellationToken);
    }
}
