using System.Threading;
using BitBenderGames;
using com.Plapamaru.Utilities;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.Layers;
using com.Plapamaru.TownCrafter.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public abstract class GameStateBuild<TContext, TFactoryEntity> : GameState<TContext>
        where TContext : GameStateBuild<TContext, TFactoryEntity>.Context
        where TFactoryEntity : FactoryEntity
    {
        [SerializeField] protected Camera _camera;
        [SerializeField] protected MobileTouchCamera _mobileTouchCamera;
        [SerializeField] protected FactorySystem _factorySystem;

        protected TFactoryEntity _entity;
        protected UIBuildPanel _buildPanel;

        private void Start()
        {
            _buildPanel = UISystem.Instance.GetPanel<UIBuildPanel>();
        }
        
        public override async UniTask Exit(CancellationToken cancellationToken)
        {
            _factorySystem.SaveEntities();
            await _buildPanel.Close(true, cancellationToken);
        }

        protected UniTask ProcessTap(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);
            
            if (FactoryUtils.TryGetMouseGridPosition(_camera, out var gridPos))
                _factorySystem.PlaceOnCenter(_entity, gridPos);
            return UniTask.CompletedTask;
        }

        protected async UniTask WaitForTouchOnEntity(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() =>
            {
                if (Input.GetMouseButton(0) == false || Utils.MouseIsOverUI())
                    return false;
                return LayersUtils.Raycast(_camera, LayerType.Interactable, out _);
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
                    _factorySystem.Place(_entity, entityStartGridPos + translation);
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

        protected UniTask ProcessSelectedButton(CancellationToken cancellationToken)
        {
            _mobileTouchCamera.SetEnabled(false);

            if (_buildPanel.SelectedButton == UIButtonType.Confirm)
            {
                if (_entity.IsCorrectlyPlaced)
                {
                    ExitBaseRun = true;
                }
            }
            else if (_buildPanel.SelectedButton == UIButtonType.RotateLeft)
            {
                _factorySystem.Rotate(_entity, -90);
            }
            else if (_buildPanel.SelectedButton == UIButtonType.RotateRight)
            {
                _factorySystem.Rotate(_entity, 90);
            }
            else if (_buildPanel.SelectedButton == UIButtonType.Close)
            {
                ExitBaseRun = true;
            }

            return UniTask.CompletedTask;
        }

        protected bool TryPlaceEntity()
        {
            var center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            if (LayersUtils.Raycast(_camera, LayerType.Ground, center, out var hit) == false)
                return false;

            _entity = InstantiateEntity();
            _entity.SetActiveInputsOutputs(true);
            _factorySystem.PlaceOnCenter(_entity, hit.point);
            _entity.ShowHighlightObject();

            return true;
        }

        protected TFactoryEntity InstantiateEntity()
        {
            var entity = FactoryMap.Instance.InstantiateEntity<TFactoryEntity>(context.id);
            entity.SetLayer(LayerType.Interactable);
            return entity;
        }

        public new abstract class Context : GameStateBase.Context
        {
            public string id;
        }
    }
}