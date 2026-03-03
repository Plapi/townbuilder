using com.Plapamaru.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIBuildPanel : UIPanel<UIBuildPanel.Data>
    {
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _rotateLeftButton;
        [SerializeField] private Button _rotateRightButton;
        [SerializeField] private Button _confirmButton;

        protected override void OnInit()
        {
            _confirmButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Confirm);
            _rotateLeftButton.SetExclusiveListener(() => SelectedButton = UIButtonType.RotateLeft);
            _rotateRightButton.SetExclusiveListener(() => SelectedButton = UIButtonType.RotateRight);
            _cancelButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Close);
        }

        public void SetRotateButtonsInteractable(bool interactable)
        {
            _rotateLeftButton.interactable = interactable;
            _rotateRightButton.interactable = interactable;

            var alpha = interactable ? 1f : 0.5f;
            _rotateLeftButton.GetComponent<CanvasGroup>().alpha = alpha;
            _rotateRightButton.GetComponent<CanvasGroup>().alpha = alpha;
        }

        public void UpdateCancelButton(bool useCancelIcon)
        {
            _cancelButton.transform.GetChild(0).gameObject.SetActive(useCancelIcon);
            _cancelButton.transform.GetChild(1).gameObject.SetActive(!useCancelIcon);
        }

        public new class Data : UIPanelBase.Data
        {

        }
    }
}