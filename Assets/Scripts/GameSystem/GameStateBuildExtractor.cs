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
    [SerializeField] private LayerMask _groundLayer;
    
    private Extractor _extractor;
    private UIBuildPanel _buildPanel;
    
    private void Start()
    {
        _buildPanel = UISystem.Instance.GetPanel<UIBuildPanel>();
        _buildPanel.Init(new UIBuildPanel.Data());
    }
    
    public new class Context : GameStateBase.Context
    {
        
    }
    
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        if (TryPlaceExtractor() == false)
            return;
        
        await _buildPanel.Show(cancellationToken);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>()
        {
            { WaitForTouchOnExtractor, ProcessExtractorPlacement },
            { WaitForCameraMovement, ProcessCameraMovement },
            { WaitForSelectedButton, ProcessSelectedButton }
        }, cancellationToken);
    }
    
    private async UniTask WaitForTouchOnExtractor(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() =>
        {
            if (Input.GetMouseButton(0) == false || Utils.MouseIsOverUI())
                return false;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out _, RAY_MAX_DISTANCE, 1 << FactorySystem.Instance.InteractableLayer);
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
                FactorySystem.Instance.Place(_extractor, extractorFirstPos + translation);
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
    
    private async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
    {
        _buildPanel.ResetSelectedButton();
        await UniTask.WaitUntil(() => _buildPanel.SelectedButton != null, cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessSelectedButton(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = false;
        
        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            if (_extractor.HasCorrectPlacement)
            {
                FactorySystem.Instance.ConfirmPlacement(_extractor);
                _extractor = null;
                await _buildPanel.Close(true, cancellationToken);
                ShouldExit = true;    
            }
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Rotate)
        {
            FactorySystem.Instance.Rotate(_extractor);
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            FactorySystem.Instance.Release(_extractor);
            await _buildPanel.Close(true, cancellationToken);
            ShouldExit = true;
        }
    }
    
    private bool TryPlaceExtractor()
    {
        var ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        if (Physics.Raycast(ray, out var hitPoint, RAY_MAX_DISTANCE, _groundLayer) == false)
            return false;
        
        _extractor = ObjectPoolingSystem.Instance.GetObject<Extractor>("Extractor");
        FactorySystem.Instance.PlaceOnCenter(_extractor, hitPoint.point);
        
        return true;
    }
}
