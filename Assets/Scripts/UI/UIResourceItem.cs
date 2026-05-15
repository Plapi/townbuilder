using UnityEngine;
using UnityEngine.UI;
using TMPro;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.Utilities;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIResourceItem : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _text;

        public void Init(ResourceItemData resourceItemData)
        {
            _image.sprite = resourceItemData.icon;
            _image.transform.localScale = Vector2.one * resourceItemData.imageScale;
            _image.transform.SetLocalY(resourceItemData.imageOffsetY);

            _text.text = resourceItemData.name;
        }
    }
}