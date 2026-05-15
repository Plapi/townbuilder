using TMPro;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(RectTransform))]
    public class UITextMeshProAutoWidth : MonoBehaviour
    {
        [SerializeField] private float _padding;
        [SerializeField] private float _minWidth;
        [SerializeField] private float _maxWidth = -1f;

        private TextMeshProUGUI _text;
        private RectTransform _rectTransform;
        private bool _isUpdatingWidth;

        private TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                    _text = GetComponent<TextMeshProUGUI>();

                return _text;
            }
        }

        private RectTransform CachedRectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();

                return _rectTransform;
            }
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            UpdateWidth();
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        private void OnValidate()
        {
            _minWidth = Mathf.Max(0f, _minWidth);

            if (!isActiveAndEnabled)
                return;

            UpdateWidth();
        }

        private void OnTextChanged(Object changedObject)
        {
            if (changedObject == Text)
                UpdateWidth();
        }

        public void UpdateWidth()
        {
            if (_isUpdatingWidth)
                return;

            if (Text == null || CachedRectTransform == null)
                return;

            _isUpdatingWidth = true;

            try
            {
                Text.ForceMeshUpdate();

                float preferredWidth = Text.GetPreferredValues(Text.text, Mathf.Infinity, CachedRectTransform.rect.height).x;
                float width = Mathf.Max(_minWidth, preferredWidth + _padding);

                if (_maxWidth >= 0f)
                    width = Mathf.Min(width, _maxWidth);

                if (!Mathf.Approximately(CachedRectTransform.rect.width, width))
                    CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
            finally
            {
                _isUpdatingWidth = false;
            }
        }
    }
}
