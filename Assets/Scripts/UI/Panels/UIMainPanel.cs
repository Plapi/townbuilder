using com.Plapamaru.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIMainPanel : UIPanel<UIMainPanel.Data>
    {
        [SerializeField] private Button _extractorButton;
        [SerializeField] private Button _conveyorButton;
        [SerializeField] private Button _craftingButton;

        protected override void OnInit()
        {
            _extractorButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Extractor);
            _conveyorButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Conveyor);
            _craftingButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Crafting);
        }

        public new class Data : UIPanelBase.Data
        {

        }
    }
}