using System.Collections.Generic;
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

        public override void OnConfirmPlacement()
        {
            base.OnConfirmPlacement();
            ReleaseHighlightObject();
            SetLayer(LayerType.Environment);
        }

        public override void OnRelease()
        {
            base.OnRelease();
            ReleaseHighlightObject();
            SetLayer(LayerType.Environment);
        }

        public void ShowHighlightObject()
        {
            if (_entityHighlightObject == null)
                _entityHighlightObject = ObjectPoolingSystem.Instance.GetObject<EntityHighlightObject>(FactoryConstants.ENTITY_HIGHLIGHT_NAME, transform.parent);
            TryPlaceHighlightObject();
        }

        public override void OnPlacementUpdate(Dictionary<Vector2Int, Entity> map)
        {
            base.OnPlacementUpdate(map);
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
    }
}
