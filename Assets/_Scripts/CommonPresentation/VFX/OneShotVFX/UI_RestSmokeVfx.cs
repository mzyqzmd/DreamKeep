using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>休息烟雾：由一组错落分布的烟团铺满顶栏以下的整个画面（网格抖动撒点、各自漂移与淡入淡出），
    /// 全部走 UI 图像通道，与场景背景同属一个画布体系。烟团数量、尺寸、漂移、错峰与总时长都在 Inspector 可调。</summary>
    public sealed class UI_RestSmokeVfx : UI_OneShotVfx
    {
        [Header("烟团")] [SerializeField] private Image _puffTemplate;
        [SerializeField] private int _columns = 5;
        [SerializeField] private int _rows = 4;
        [SerializeField] private Vector2 _puffSize = new(520f, 760f);
        [SerializeField] private Vector2 _puffAlpha = new(0.05f, 0.11f);
        [SerializeField] private Vector2 _puffScale = new(0.8f, 1.5f);
        [SerializeField] private float _puffRise = 160f;
        [SerializeField] private float _puffDrift = 200f;
        [SerializeField] private float _puffLife = 2.1f;
        [SerializeField] private float _puffDelay = 0.9f;
        [SerializeField] private float _puffInset = 60f;
        [SerializeField] private float _topReservedPx = 170f;

        [Header("总时长")] [SerializeField] private float _duration = 2.2f;

        private readonly List<Image> _puffs = new();
        private readonly List<Tween> _tweens = new();

        protected override float Duration => _duration;

        protected override void Awake()
        {
            base.Awake();
            if (_puffTemplate) _puffTemplate.gameObject.SetActive(false);
        }

        // 还池/收段即中止：杀全部补间、清掉未销毁的烟团，防池复用串场
        private void OnDisable()
        {
            for (int i = _tweens.Count - 1; i >= 0; i--) _tweens[i]?.Kill();
            _tweens.Clear();
            for (int i = _puffs.Count - 1; i >= 0; i--)
                if (_puffs[i]) Destroy(_puffs[i].gameObject);
            _puffs.Clear();
        }

        protected override void PlayOnce()
        {
            SpawnPuffs();
        }

        #region 对外播放：多团小烟铺满顶栏以下

        // 网格抖动撒点：顶栏以下切成 _columns × _rows 格，每格随机落一团，保证没有大片空洞
        private void SpawnPuffs()
        {
            if (!_puffTemplate) return;
            RectTransform root = (RectTransform)transform;
            Vector2 size = root.rect.size;
            if (size.x <= 0f || size.y <= 0f) size = new Vector2(2560f, 1440f);

            float halfW = Mathf.Max(1f, size.x * 0.5f - _puffInset);
            float top = size.y * 0.5f - _topReservedPx;
            float bottom = -size.y * 0.5f + _puffInset;
            if (top <= bottom) top = bottom + 1f;

            float cellW = halfW * 2f / Mathf.Max(1, _columns);
            float cellH = (top - bottom) / Mathf.Max(1, _rows);

            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    float x = -halfW + (column + 0.5f) * cellW +
                              UnityEngine.Random.Range(-cellW * 0.5f, cellW * 0.5f);
                    float y = bottom + (row + 0.5f) * cellH +
                              UnityEngine.Random.Range(-cellH * 0.5f, cellH * 0.5f);
                    Image puff = Instantiate(_puffTemplate, transform);
                    puff.gameObject.SetActive(true);
                    puff.rectTransform.sizeDelta = Vector2.one *
                        UnityEngine.Random.Range(_puffSize.x, _puffSize.y);
                    puff.rectTransform.localRotation =
                        Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f));
                    puff.sprite = _puffTemplate.sprite;
                    PlayPuff(puff, new Vector2(x, y), UnityEngine.Random.Range(0f, _puffDelay));
                }
            }
        }

        #endregion

        // 单团烟雾：延迟后淡入到峰值，同时向侧上方飘、缓慢放大，再淡出销毁
        private void PlayPuff(Image puff_, Vector2 origin_, float delay_)
        {
            RectTransform rect = puff_.rectTransform;
            rect.anchoredPosition = origin_;
            float baseScale = UnityEngine.Random.Range(_puffScale.x, _puffScale.y);
            rect.localScale = Vector3.one * (baseScale * 0.8f);
            float peak = UnityEngine.Random.Range(_puffAlpha.x, _puffAlpha.y);
            puff_.color = WithAlpha(puff_.color, 0f);

            float duration = _puffLife * UnityEngine.Random.Range(0.85f, 1.25f);
            Vector2 drift = new(
                UnityEngine.Random.Range(-_puffDrift, _puffDrift),
                _puffRise * UnityEngine.Random.Range(0.6f, 1.2f));

            Sequence sequence = DOTween.Sequence().SetDelay(delay_);
            sequence.Join(puff_.DOFade(peak, duration * 0.4f).SetEase(Ease.OutQuad));
            sequence.Join(rect.DOAnchorPos(origin_ + drift, duration).SetEase(Ease.OutSine));
            sequence.Join(rect.DOScale(baseScale * 1.35f, duration).SetEase(Ease.OutQuad));
            sequence.Insert(duration * 0.4f, puff_.DOFade(0f, duration * 0.6f).SetEase(Ease.InQuad));
            sequence.OnComplete(() => DestroyPuff(puff_));

            _puffs.Add(puff_);
            _tweens.Add(sequence);
        }

        private void DestroyPuff(Image puff_)
        {
            _puffs.Remove(puff_);
            if (puff_) Destroy(puff_.gameObject);
        }

        private static Color WithAlpha(Color color_, float alpha_) => new(color_.r, color_.g, color_.b, alpha_);
    }
}
