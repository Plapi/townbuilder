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
            UpdateUI(_data.entity.Data);
        }

        private void UpdateUI(EntityData entityData)
        {
            _nameText.text = entityData.name;
            _descriptionText.text = entityData.description;
            _image.sprite = entityData.icon;
            _image.transform.localScale = Vector2.one * entityData.imageScale;
        }

        public new class Data : UIPanelBase.Data
        {
            public Entity entity;
        }

#if UNITY_EDITOR

        [Space]
        [SerializeField] private EntityData _debugEntityData;

        [ContextMenu("Update UI Debug Data")]
        private void UpdateUIDebugData()
        {
            UpdateUI(_debugEntityData);
        }

#endif
    }
}