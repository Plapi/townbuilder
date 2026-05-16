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
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Init(ResourceItemData resourceItemData)
        {
            _image.sprite = resourceItemData.icon;
            _image.transform.localScale = Vector2.one * resourceItemData.imageScale;
            _image.GetComponent<RectTransform>().SetAnchorPosY(resourceItemData.imageOffsetY);

            _nameText.text = resourceItemData.name;
        }

        public void Init(ResourceItemData resourceItemData, int amount)
        {
            Init(resourceItemData);

            if (_amountText != null)
                _amountText.text = amount.ToString();
        }
    }
}