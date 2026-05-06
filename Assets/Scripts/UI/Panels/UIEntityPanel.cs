using com.Plapamaru.TownCrafter.Factory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIEntityPanel : UIPanel<UIEntityPanel.Data>
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Image _image;

        protected override void OnInit()
        {
            _nameText.text = _data.entity.Data.name;
            _descriptionText.text = _data.entity.Data.description;
            _image.sprite = _data.entity.Data.icon;
        }

        public new class Data : UIPanelBase.Data
        {
            public Entity entity;
        }
    }
}