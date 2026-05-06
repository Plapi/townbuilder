using System.Threading;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIMainPanel : UIPanel<UIMainPanel.Data>
    {
        [SerializeField] private RectTransform _bottom;
        [SerializeField] private Button _extractorButton;
        [SerializeField] private Button _conveyorButton;
        [SerializeField] private Button _crafterButton;
        [SerializeField] private Button _deleteButton;

        protected override void OnInit()
        {
            _extractorButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Extractor);
            _conveyorButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Conveyor);
            _crafterButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Crafter);
            _deleteButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Delete);
        }

        protected override async UniTask ShowAnim(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            _bottom.DOKill();
            await _bottom
                .DOMoveY(0f, UISystem.DEFAULT_TIME)
                .SetEase(Ease.OutCubic)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        protected override async UniTask CloseAnim(bool anim, CancellationToken cancellationToken)
        {
            if (anim)
            {
                _bottom.DOKill();
                await _bottom
                    .DOMoveY(-300f, UISystem.DEFAULT_TIME)
                    .SetEase(Ease.InCubic)
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            gameObject.SetActive(false);
        }

        public new class Data : UIPanelBase.Data
        {

        }
    }
}