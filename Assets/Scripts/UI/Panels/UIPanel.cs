using System.Threading;
using com.Plapamaru;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace com.Plapamaru.TownCrafter.UI
{
    public abstract class UIPanel<T> : UIPanelBase where T : UIPanelBase.Data
    {
        [SerializeField] protected Image _background;
        [SerializeField] protected CanvasGroup _content;
        [SerializeField] private Button[] _closeButtons;
        protected T _data { get; private set; }

        public UIButtonType? SelectedButton { get; protected set; }

        public void Init(T data)
        {
            _data = data;

            foreach (var button in _closeButtons)
                button.SetExclusiveListener(() => SelectedButton = UIButtonType.Close);

            OnInit();
        }

        protected abstract void OnInit();

        public async UniTask Show(CancellationToken cancellationToken)
        {
            await ShowAnim(cancellationToken);
        }

        public async UniTask Close(bool anim, CancellationToken cancellationToken)
        {
            await CloseAnim(anim, cancellationToken);
        }

        protected virtual async UniTask ShowAnim(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            if (_background != null && _content != null)
            {
                _background.SetAlpha(0f);
                _content.transform.DOPunchScale(Vector3.one * 0.2f, UISystem.DEFAULT_TIME).SetUpdate(true);
                await _background.DOFade(1f, UISystem.DEFAULT_TIME).SetUpdate(true).ToUniTask(cancellationToken: cancellationToken);
            }
        }

        protected virtual async UniTask CloseAnim(bool anim, CancellationToken cancellationToken)
        {
            if (anim && _background != null && _content != null)
            {
                await UniTask.WhenAll(
                    _content.transform
                        .DOScale(Vector3.one * 0.5f, UISystem.DEFAULT_TIME)
                        .SetEase(Ease.InQuad)
                        .ToUniTask(cancellationToken: cancellationToken),
                    _content
                        .DOFade(0f, UISystem.DEFAULT_TIME)
                        .ToUniTask(cancellationToken: cancellationToken),
                    _background.DOFade(0f, UISystem.DEFAULT_TIME).ToUniTask(cancellationToken: cancellationToken));

                _content.transform.localScale = Vector3.one;
                _content.alpha = 1f;
            }

            gameObject.SetActive(false);
        }

        public void ResetSelectedButton()
        {
            SelectedButton = null;
        }
    }

    public abstract class UIPanelBase : UIObject
    {
        public abstract class Data
        {

        }
    }
}