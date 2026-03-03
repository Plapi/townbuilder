using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIObject : MonoBehaviour
    {

        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }
    }
}