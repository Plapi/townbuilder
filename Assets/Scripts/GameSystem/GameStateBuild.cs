using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class GameStateBuild<TContext, TFactoryEntity> : GameState<TContext>
    where TContext : GameStateBuild<TContext, TFactoryEntity>.Context
    where TFactoryEntity : FactoryEntity
{
    [SerializeField] protected Camera _camera;
    [SerializeField] protected MobileTouchCamera _mobileTouchCamera;
    
    protected TFactoryEntity _entity;
    protected UIBuildPanel _buildPanel;
    protected bool _confirmEntityPlacement;
    
    private void Start()
    {
        _buildPanel = UISystem.Instance.GetPanel<UIBuildPanel>();
    }
    
    protected async UniTask WaitForTouchOnEntity(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() =>
        {
            if (Input.GetMouseButton(0) == false || Utils.MouseIsOverUI())
                return false;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out _, Constants.RAY_MAX_DISTANCE, 1 << Constants.InteractableLayer);
        }, cancellationToken: cancellationToken);
    }
    
    protected async UniTask ProcessEntityPlacement(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        
        if (FactoryUtils.TryGetMouseGridPosition(_camera, out var firstGridPos) == false)
            return;
        
        var entityStartGridPos = new Vector2Int(Mathf.RoundToInt(_entity.transform.position.x), Mathf.RoundToInt(_entity.transform.position.z));
        
        while (Input.GetMouseButton(0) && cancellationToken.IsCancellationRequested == false)
        {
            if (FactoryUtils.TryGetMouseGridPosition(_camera, out var gridPos))
            {
                var translation = gridPos - firstGridPos;
                FactorySystem.Instance.Place(_entity, entityStartGridPos + translation);
            }
            await UniTask.NextFrame(cancellationToken: cancellationToken);
        }
    }
    
    protected async UniTask WaitForCameraMovement(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(true);
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging || _mobileTouchCamera.IsPinching, cancellationToken: cancellationToken);
    }
    
    protected async UniTask ProcessCameraMovement(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging == false && _mobileTouchCamera.IsPinching == false, cancellationToken: cancellationToken);
    }
    
    protected async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
    {
        _buildPanel.ResetSelectedButton();
        await UniTask.WaitUntil(() => _buildPanel.SelectedButton != null, cancellationToken: cancellationToken);
    }
    
    protected UniTask ProcessSelectedButton(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        
        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            if (_entity.IsCorrectlyPlaced)
            {
                ShouldExit = true;
                _confirmEntityPlacement = true;
            }
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Rotate)
        {
            FactorySystem.Instance.Rotate(_entity);
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            ShouldExit = true;
            _confirmEntityPlacement = false;
        }
        
        return UniTask.CompletedTask;
    }
    
    protected bool TryPlaceEntity()
    {
        var ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        if (Physics.Raycast(ray, out var hitPoint, Constants.RAY_MAX_DISTANCE, 1 << Constants.GroundLayer) == false)
            return false;
        
        _entity = InstantiateEntity();
        _entity.SetActiveInputsOutputs(true);
        FactorySystem.Instance.PlaceOnCenter(_entity, hitPoint.point);
        
        return true;
    }

    protected TFactoryEntity InstantiateEntity()
    {
        var entity = ObjectPoolingSystem.Instance.GetObject<TFactoryEntity>(context.id);
        entity.gameObject.SetLayerRecursively(Constants.InteractableLayer);
        return entity;
    }
    
    public new abstract class Context : GameStateBase.Context
    {
        public string id;
    }
}
