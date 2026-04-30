using com.Plapamaru.Utilities;
using com.Plapamaru.Pooling;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class EntityHighlightObject : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Transform _forwardLeftCorner;
        [SerializeField] private Transform _forwardRightCorner;
        [SerializeField] private Transform _backRightCorner;

        public string Id => FactoryConstants.ENTITY_HIGHLIGHT_NAME;
        public MonoBehaviour Behaviour => this;

        public void Place(Entity entity, Color color)
        {
            transform.position = entity.transform.position;
            transform.SetAngleY(entity.transform.eulerAngles.y);

            _forwardLeftCorner.SetLocalZ(entity.Size.y);
            _forwardRightCorner.SetLocalXZ(entity.Size.x, entity.Size.y);
            _backRightCorner.SetLocalX(entity.Size.x);

            SetColor(color);
        }

        public void Place(Vector2Int gridPos, Color color)
        {
            transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
            _forwardLeftCorner.SetLocalZ(1f);
            _forwardRightCorner.SetLocalXZ(1f, 1f);
            _backRightCorner.SetLocalX(1f);
            SetColor(color);
        }

        private void SetColor(Color color)
        {
            foreach (var rend in _renderers)
                rend.material.color = color;
        }

        public void OnDispose()
        {

        }
    }
}