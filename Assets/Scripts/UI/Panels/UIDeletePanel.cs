using com.Plapamaru.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIDeletePanel : UIPanel<UIDeletePanel.Data>
    {
        [SerializeField] private Button _closeButton;

        protected override void OnInit()
        {
            _closeButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Close);
        }

        public new class Data : UIPanelBase.Data
        {

        }
    }
}