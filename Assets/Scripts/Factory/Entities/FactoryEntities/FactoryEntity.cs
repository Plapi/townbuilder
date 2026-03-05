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

        public bool IsCorrectlyPlaced { get; private set; }

        public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
        {
            IsCorrectlyPlaced = hasCorrectPlacement;

            if (_entityHighlightObject == null)
                _entityHighlightObject = ObjectPoolingSystem.Instance.GetObject<EntityHighlightObject>(FactoryConstants.ENTITY_HIGHLIGHT_NAME, transform.parent);

            _entityHighlightObject.Place(this, hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor);
        }

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
