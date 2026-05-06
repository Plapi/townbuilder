using com.Plapamaru.Utilities;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class MultipleHighlightSelection : MonoBehaviour
    {
        [SerializeField] private Transform _backLeftCorner;
        [SerializeField] private Transform _forwardLeftCorner;
        [SerializeField] private Transform _forwardRightCorner;
        [SerializeField] private Transform _backRightCorner;

        public void SetFromEntity(Entity entity)
        {
            transform.position = entity.transform.position;
            transform.SetAngleY(entity.transform.eulerAngles.y);

            float sx = entity.Size.x;
            float sy = entity.Size.y;
            _forwardLeftCorner.SetLocalZ(sy);
            _forwardRightCorner.SetLocalXZ(sx, sy);
            _backRightCorner.SetLocalX(sx);
            _backLeftCorner.SetLocalXYZ(0f, _backLeftCorner.localPosition.y, 0f);
        }

        public void SetFromGridRange(Vector2Int first, Vector2Int last)
        {
            int minX = Mathf.Min(first.x, last.x);
            int maxX = Mathf.Max(first.x, last.x);
            int minZ = Mathf.Min(first.y, last.y);
            int maxZ = Mathf.Max(first.y, last.y);

            float widthCells = maxX - minX + 1f;
            float depthCells = maxZ - minZ + 1f;

            transform.position = new Vector3(minX, 0f, minZ);

            _forwardLeftCorner.SetLocalZ(depthCells);
            _forwardRightCorner.SetLocalXZ(widthCells, depthCells);
            _backRightCorner.SetLocalX(widthCells);

            if (_backLeftCorner != null)
            {
                float y = _backLeftCorner.localPosition.y;
                _backLeftCorner.SetLocalXYZ(0f, y, 0f);
            }
        }
    }
}