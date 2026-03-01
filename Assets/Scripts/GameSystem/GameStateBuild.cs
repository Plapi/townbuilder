using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class GameStateBuild<TContext, TFactoryEntity> : GameState<TContext>
    where TContext : GameStateBuild<TContext, TFactoryEntity>.Context
    where TFactoryEntity : FactoryEntity
{

    private const float TAP_MAX_TIME = 0.2f;
    
    [SerializeField] protected Camera _camera;
    [SerializeField] protected MobileTouchCamera _mobileTouchCamera;
    
    protected TFactoryEntity _entity;
    protected UIBuildPanel _buildPanel;
    
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
            return Layers.Raycast(_camera, Layers.InteractableLayer, out _);
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
        await UniTask.WaitUntil(() => _mobileTouchCamera.HasInteraction, cancellationToken: cancellationToken);
    }
    
    protected async UniTask ProcessCameraMovement(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => _mobileTouchCamera.HasInteraction == false, cancellationToken: cancellationToken);
    }
    
    protected async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
    {
        _buildPanel.ResetSelectedButton();
        await UniTask.WaitUntil(() => _buildPanel.SelectedButton != null, cancellationToken: cancellationToken);
    }
    
    protected virtual UniTask ProcessSelectedButton(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.SetEnabled(false);
        
        if (_buildPanel.SelectedButton == UIButtonType.Confirm)
        {
            if (_entity.IsCorrectlyPlaced)
            {
                ExitBaseRun = true;
            }
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Rotate)
        {
            FactorySystem.Instance.Rotate(_entity);
        }
        else if (_buildPanel.SelectedButton == UIButtonType.Close)
        {
            ExitBaseRun = true;
        }
        
        return UniTask.CompletedTask;
    }

    protected async UniTask WaitingForTap(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && Utils.MouseIsOverUI() == false, cancellationToken: cancellationToken);
            float time = Time.time;
            await UniTask.WaitUntil(() => Time.time > time + TAP_MAX_TIME || Input.GetMouseButtonUp(0) || _mobileTouchCamera.HasInteraction, 
                cancellationToken: cancellationToken);
            if (_mobileTouchCamera.HasInteraction == false && Time.time <= time + TAP_MAX_TIME)
                return;
        }
    }

    protected bool TryPlaceEntity()
    {
        var center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        if (Layers.Raycast(_camera, Layers.GroundLayer, center, out var hit) == false)
            return false;
        
        _entity = InstantiateEntity();
        _entity.SetActiveInputsOutputs(true);
        FactorySystem.Instance.PlaceOnCenter(_entity, hit.point);
        
        return true;
    }

    protected TFactoryEntity InstantiateEntity()
    {
        var entity = FactorySystem.Instance.InstantiateEntity<TFactoryEntity>(context.id);
        entity.gameObject.SetLayerRecursively(Layers.InteractableLayer);
        return entity;
    }
    
    public new abstract class Context : GameStateBase.Context
    {
        public string id;
    }
}
