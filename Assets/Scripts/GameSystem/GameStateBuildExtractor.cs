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
    
    [Space]
    [SerializeField] private LayerMask _groundLayer;
    
    private LayerMask _interactableLayer;
    private Extractor _extractor;
    private UIBuildPanel _buildPanel;
    
    private void Start()
    {
        _interactableLayer = LayerMask.NameToLayer(Constants.INTERACTABLE_LAYER_NAME);
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
            return Physics.Raycast(ray, out _, RAY_MAX_DISTANCE, 1 << _interactableLayer);
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
            ReleaseExtractor();
            await _buildPanel.Close(true, cancellationToken);
            ShouldExit = true;
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Rotate)
        {
            _extractor.Rotate();
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            ObjectPoolingSystem.Instance.ReleaseObject(_extractor);
            ReleaseExtractor();
            
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
        _extractor.PlaceOnCenter(hitPoint.point);
        _extractor.gameObject.SetLayerRecursively(_interactableLayer);
        
        return true;
    }
    
    private void ReleaseExtractor()
    {
        _extractor.gameObject.SetLayerRecursively(0);
        _extractor = null;
    }
}
