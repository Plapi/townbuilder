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

        protected override void Awake()
        {
            base.Awake();
            SetActiveInputsOutputs(false);
        }

        public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
        {
            IsCorrectlyPlaced = hasCorrectPlacement;

            if (_entityHighlightObject == null)
                _entityHighlightObject = ObjectPoolingSystem.Instance.GetObject<EntityHighlightObject>(FactoryConstants.ENTITY_HIGHLIGHT_NAME, transform.parent);

            _entityHighlightObject.Place(this, hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor);
        }

        public void SetActiveInputsOutputs(bool active)
        {
            foreach (var input in _inputs)
                input.gameObject.SetActive(active);
            foreach (var output in _outputs)
                output.gameObject.SetActive(active);
        }

        public override void OnConfirmPlacement()
        {
            base.OnConfirmPlacement();
            SetActiveInputsOutputs(false);
            ReleaseHighlightObject();
            SetLayer(LayerType.Environment);
        }

        public override void OnRelease()
        {
            base.OnRelease();
            SetActiveInputsOutputs(false);
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
