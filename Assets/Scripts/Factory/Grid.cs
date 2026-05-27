using DG.Tweening;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Grid : MonoBehaviour
    {
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private float _fadeDuration = 0.25f;

        private MaterialPropertyBlock _propertyBlock;
        private Color[][] _defaultColors;
        private float _alpha;
        private Tween _fadeTween;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            Init();
            SetAlpha(0f);
            SetRenderersEnabled(false);
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
        }

        public void FadeIn()
        {
            Fade(1f);
        }

        public void FadeOut()
        {
            Fade(0f);
        }

        private void Init()
        {
            _defaultColors = new Color[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];

                var materials = renderer.sharedMaterials;
                _defaultColors[i] = new Color[materials.Length];

                for (int j = 0; j < materials.Length; j++)
                    _defaultColors[i][j] = GetMaterialColor(materials[j]);
            }
        }

        private void Fade(float targetAlpha)
        {
            _fadeTween?.Kill();

            if (targetAlpha > 0f)
                SetRenderersEnabled(true);

            _fadeTween = DOTween
                .To(() => _alpha, SetAlpha, targetAlpha, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (Mathf.Approximately(targetAlpha, 0f))
                        SetRenderersEnabled(false);
                });
        }

        private void SetAlpha(float alpha)
        {
            _alpha = alpha;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                var defaultColors = _defaultColors?[i];
                if (renderer == null || defaultColors == null)
                    continue;

                var materials = renderer.sharedMaterials;
                for (int j = 0; j < defaultColors.Length; j++)
                {
                    var material = materials[j];
                    if (material == null)
                        continue;

                    renderer.GetPropertyBlock(_propertyBlock, j);
                    var color = defaultColors[j];
                    color.a *= alpha;

                    if (material.HasProperty(BaseColorID))
                        _propertyBlock.SetColor(BaseColorID, color);
                    if (material.HasProperty(ColorID))
                        _propertyBlock.SetColor(ColorID, color);

                    renderer.SetPropertyBlock(_propertyBlock, j);
                }
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            foreach (var renderer in _renderers)
                renderer.enabled = enabled;
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material == null)
                return Color.white;

            if (material.HasProperty(BaseColorID))
                return material.GetColor(BaseColorID);

            if (material.HasProperty(ColorID))
                return material.GetColor(ColorID);

            return Color.white;
        }
    }
}