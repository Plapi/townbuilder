using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Singletons;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UISystem : MonoBehaviourSingleton<UISystem>
    {
        public const float DEFAULT_TIME = 0.2f;

        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private GameObject _loading;
        [SerializeField] private Image _fadeToBlackImage;

        private readonly Dictionary<Type, UIPanelBase> _dictPanels = new Dictionary<Type, UIPanelBase>();

        public Vector2 Size => _canvasScaler.referenceResolution;

        protected override void Awake()
        {
            base.Awake();
            var panels = GetComponentsInChildren<UIPanelBase>(true);
            foreach (var panel in panels)
            {
                _dictPanels.Add(panel.GetType(), panel);
                panel.gameObject.SetActive(false);
            }
        }

        public T GetPanel<T>() where T : UIPanelBase
        {
            return _dictPanels[typeof(T)] as T;
        }

        public async UniTask FadeInToBlack(CancellationToken cancellationToken)
        {
            _fadeToBlackImage.SetAlpha(0f);
            _fadeToBlackImage.gameObject.SetActive(true);
            await _fadeToBlackImage.DOFade(1f, 0.2f).SetUpdate(true).ToUniTask(cancellationToken: cancellationToken);
        }

        public async UniTask FadeOutFromBlack(CancellationToken cancellationToken)
        {
            await _fadeToBlackImage.DOFade(0f, 0.2f).SetUpdate(true).ToUniTask(cancellationToken: cancellationToken);
            _fadeToBlackImage.gameObject.SetActive(false);
        }

        public void ShowLoading()
        {
            _loading.SetActive(true);
        }

        public void HideLoading()
        {
            _loading.SetActive(false);
        }
    }
}