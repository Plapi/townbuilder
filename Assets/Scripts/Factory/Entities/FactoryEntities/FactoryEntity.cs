using com.Plapamaru.Pooling;
using com.Plapamaru.TownCrafter.Layers;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public abstract class FactoryEntity : Entity
    {
        [SerializeField] protected GameObject _graphic;

        [Header("Runtime Properties")]
        [SerializeField] private EntityHighlightObject _entityHighlightObject;

        protected ResourceItem _resourceItem;

        public override void OnConfirmPlacement()
        {
            base.OnConfirmPlacement();
            ReleaseHighlightObject();
            SetLayer(LayerType.Environment);
        }

        public override void OnDispose()
        {
            base.OnDispose();
            ReleaseHighlightObject();
            SetLayer(LayerType.Environment);
            if (_resourceItem != null)
            {
                ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
                _resourceItem = null;
            }
        }

        public bool HasResourceItem()
        {
            return _resourceItem != null;
        }

        public void ShowHighlightObject()
        {
            if (_entityHighlightObject == null)
                _entityHighlightObject = ObjectPoolingSystem.Instance.GetObject<EntityHighlightObject>(FactoryConstants.ENTITY_HIGHLIGHT_NAME, transform.parent);
            TryPlaceHighlightObject();
        }

        public override void OnPlacementUpdate()
        {
            base.OnPlacementUpdate();
            TryPlaceHighlightObject();
        }

        private void TryPlaceHighlightObject()
        {
            if (_entityHighlightObject != null)
                _entityHighlightObject.Place(this, IsCorrectlyPlaced ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor);
        }

        public void ReleaseHighlightObject()
        {
            if (_entityHighlightObject != null)
            {
                ObjectPoolingSystem.Instance.ReleaseObject(_entityHighlightObject);
                _entityHighlightObject = null;
            }
        }

        protected void PassResourceItem(FactoryEntity passToEntity)
        {
            passToEntity._resourceItem = _resourceItem;
            _resourceItem.transform.parent = passToEntity.transform;
            _resourceItem = null;
        }
    }

    public abstract class FactoryEntity<TSaveData> : FactoryEntity where TSaveData : EntitySaveData, new()
    {
        public virtual TSaveData ToSaveData()
        {
            return new TSaveData()
            {
                id = Id,
                gridPos = GridPos,
                rotationY = Mathf.RoundToInt(transform.eulerAngles.y),
            };
        }
    }
}