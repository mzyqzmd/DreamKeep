using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>锻造锤击：预制体根上带 Image(圆环) + Animator(VFX_SmithOnce) + Mark/Spark 子节点。
    /// 每击重启圆环动画、随机落点的高光斑、一簇外抛火花，再叠一层扩散冲击环；第三击自身轻震收尾。
    /// 三段播完整体渐隐；取池/摆位/到时还池沿 UI_OneShotVfx 契约，本件只负责"怎么播、播多久"。</summary>
    public sealed class UI_SmithForgeVfx : UI_OneShotVfx
    {
        private const string HitStateName = "VFX_SmithOnce"; // AC_Smith 唯一 state 名，写错会静默不播

        [Header("锤击")] [SerializeField] private int _hitCount = 3;
        [SerializeField] private float _hitInterval = 0.42f;
        [SerializeField] private float _fadeOutDuration = 0.55f;

        [Header("火花")] [SerializeField] private int _sparkCount = 22;
        [SerializeField] private float _sparkDuration = 0.6f;
        [SerializeField] private float _sparkSpread = 260f;

        [Header("落点光斑")] [SerializeField] private float _markFadeDelay = 0.16f;
        [SerializeField] private float _markFadeDuration = 0.32f;
        [SerializeField] private float _markFromScale = 0.75f;
        [SerializeField] private float _markToScale = 1.8f;
        [SerializeField] private float _markAreaHalf = 170f;

        [Header("冲击环")] [SerializeField] private Color _impactColor = Color.white;
        [SerializeField] private float _impactAlpha = 0.9f;
        [SerializeField] private float _impactFromScale = 0.5f;
        [SerializeField] private float _impactToScale = 2.6f;
        [SerializeField] private float _impactSeconds = 0.32f;

        [Header("重击")] [SerializeField] private float _shakeSeconds = 0.18f;
        [SerializeField] private float _shakeStrength = 28f;

        private readonly List<Graphic> _sparks = new();
        private Graphic _mark;
        private Graphic _sparkTemplate;
        private CanvasGroup _canvasGroup;
        private Animator _forgeAnimator;
        private Image _impact;
        private Tween _markTween;
        private Tween _impactTween;
        private Tween _shakeTween;
        private Tween _fadeTween;
        private Vector2 _baseAnchoredPosition;
        private int _hitTicket;

        protected override void Awake()
        {
            base.Awake(); // 基类缓存根 Image 并把 alpha 归零，圆环只在播放期间可见
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _forgeAnimator = GetComponent<Animator>();
            _mark = transform.Find("Mark")?.GetComponent<Graphic>();
            _sparkTemplate = transform.Find("Spark")?.GetComponent<Graphic>();
            BuildLayers();
        }

        protected override float Duration => _hitCount * _hitInterval + _fadeOutDuration;

        protected override void PlayOnce()
        {
            _baseAnchoredPosition = ((RectTransform)transform).anchoredPosition;
            ResetVisual();
            HitLoopAsync(++_hitTicket).Forget();
        }

        private void OnDisable() => Abort();

        private void OnDestroy()
        {
            _markTween?.Kill();
            _impactTween?.Kill();
            _shakeTween?.Kill();
            _fadeTween?.Kill();
        }

        // 冲击环：一份 Image 在 Awake 建一次随实例长住，避免每击生成销毁
        private void BuildLayers()
        {
            Sprite baseSprite = TryGetComponent(out Image rootImage) ? rootImage.sprite : null;
            _impact = CreateLayer("Impact", baseSprite, 1f);
            _impact.transform.SetSiblingIndex(0); // 冲击环压在落点光斑与火花之下
        }

        // 统一造层：锚点与 pivot 居中、不吃射线、初始全透明
        private Image CreateLayer(string name_, Sprite sprite_, float sizeMultiplier_)
        {
            GameObject layerObject = new(name_);
            RectTransform layerRect = layerObject.AddComponent<RectTransform>();
            layerRect.SetParent(transform, false);
            layerRect.anchorMin = layerRect.anchorMax = layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.anchoredPosition = Vector2.zero;
            layerRect.sizeDelta = sprite_ ? sprite_.rect.size : ((RectTransform)transform).sizeDelta * sizeMultiplier_;

            Image layer = layerObject.AddComponent<Image>();
            layer.sprite = sprite_;
            layer.raycastTarget = false;
            layer.color = WithAlpha(Color.white, 0f);
            return layer;
        }

        private void ResetVisual()
        {
            _canvasGroup.alpha = 1f;
            if (_mark) _mark.color = WithAlpha(_mark.color, 0f);
            if (_sparkTemplate) _sparkTemplate.gameObject.SetActive(false);
            if (_impact)
            {
                _impact.color = WithAlpha(_impactColor, 0f);
                _impact.rectTransform.localScale = Vector3.one * _impactFromScale;
            }
        }

        private async UniTaskVoid HitLoopAsync(int ticket_)
        {
            for (int hit = 0; hit < _hitCount; hit++)
            {
                if (ticket_ != _hitTicket) return;
                PlayHit(hit == _hitCount - 1);
                await UniTask.Delay(TimeSpan.FromSeconds(_hitInterval), cancellationToken: destroyCancellationToken)
                    .SuppressCancellationThrow();
            }

            if (ticket_ != _hitTicket) return;
            await FadeOutAsync();
        }

        private void PlayHit(bool heavy_)
        {
            if (_forgeAnimator) _forgeAnimator.Play(HitStateName, 0, 0f);
            PlayImpact();
            ShowMarkRandom();
            SpawnSparks();
            if (!heavy_) return;
            PlayShake();
        }

        // 扩散冲击环：从核心向外放大同时淡出，放大终点盖过圆环动画本身
        private void PlayImpact()
        {
            if (!_impact) return;
            _impactTween?.Kill();
            _impact.rectTransform.localScale = Vector3.one * _impactFromScale;
            _impact.color = WithAlpha(_impactColor, _impactAlpha);
            Sequence impactSequence = DOTween.Sequence();
            impactSequence.Join(_impact.rectTransform.DOScale(_impactToScale, _impactSeconds).SetEase(Ease.OutQuad));
            impactSequence.Join(_impact.DOFade(0f, _impactSeconds).SetEase(Ease.InQuad));
            _impactTween = impactSequence;
        }

        private void ShowMarkRandom()
        {
            if (!_mark) return;
            _markTween?.Kill();
            ((RectTransform)_mark.transform).anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-_markAreaHalf, _markAreaHalf),
                UnityEngine.Random.Range(-_markAreaHalf, _markAreaHalf));
            _mark.color = WithAlpha(_mark.color, 1f);
            _mark.rectTransform.localScale = Vector3.one * _markFromScale;
            Sequence markSequence = DOTween.Sequence();
            markSequence.Join(_mark.DOFade(0f, _markFadeDuration).SetDelay(_markFadeDelay));
            markSequence.Join(_mark.rectTransform.DOScale(_markToScale, _markFadeDuration).SetDelay(_markFadeDelay)
                .SetEase(Ease.OutQuad));
            _markTween = markSequence;
        }

        private void SpawnSparks()
        {
            if (!_sparkTemplate) return;
            RectTransform baseRect = (RectTransform)_sparkTemplate.transform;
            for (int i = 0; i < _sparkCount; i++)
            {
                Graphic spark = Instantiate(_sparkTemplate, transform);
                spark.gameObject.SetActive(true);
                RectTransform rect = (RectTransform)spark.transform;
                rect.anchoredPosition = baseRect.anchoredPosition;
                rect.localScale = Vector3.one;
                spark.color = WithAlpha(spark.color, 1f);
                Vector2 dir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, -0.15f))
                    .normalized;
                Vector2 end = rect.anchoredPosition + dir * (_sparkSpread * UnityEngine.Random.Range(0.55f, 1f));
                rect.DOAnchorPos(end, _sparkDuration).SetEase(Ease.OutQuad);
                rect.DOScale(0.35f, _sparkDuration).SetEase(Ease.InQuad);
                rect.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(120f, 360f)), _sparkDuration,
                    RotateMode.FastBeyond360);
                spark.DOFade(0f, _sparkDuration).SetEase(Ease.InQuad);
                _sparks.Add(spark);
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_sparkDuration), cancellationToken: destroyCancellationToken)
                        .SuppressCancellationThrow();
                    if (spark) DestroySpark(spark);
                });
            }
        }

        // 只抖自身节点，不动父级布局；每次开播前记下基准位，收尾与中止都复位
        private void PlayShake()
        {
            if (transform is not RectTransform rootRect) return;
            _shakeTween?.Kill();
            rootRect.anchoredPosition = _baseAnchoredPosition;
            _shakeTween = rootRect.DOShakePosition(_shakeSeconds, _shakeStrength, 16);
        }

        private async UniTask FadeOutAsync()
        {
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, 0f, _fadeOutDuration);
            await UniTask.Delay(TimeSpan.FromSeconds(_fadeOutDuration), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();
        }

        // 还池/收段即中止：票号作废本轮循环，杀补间、复位抖动位移并清掉未落地的火花，防池复用串场
        private void Abort()
        {
            _hitTicket++;
            _markTween?.Kill();
            _impactTween?.Kill();
            _shakeTween?.Kill();
            _fadeTween?.Kill();
            if (transform is RectTransform rootRect) rootRect.anchoredPosition = _baseAnchoredPosition;
            // 倒序销毁：DestroySpark 会从列表移除，正序 foreach 会因集合被改而抛异常
            for (int i = _sparks.Count - 1; i >= 0; i--)
                if (_sparks[i]) DestroySpark(_sparks[i]);
            _sparks.Clear();
        }

        private void DestroySpark(Graphic spark_)
        {
            _sparks.Remove(spark_);
            spark_.DOKill();
            Destroy(spark_.gameObject);
        }

        private static Color WithAlpha(Color color_, float alpha_) => new(color_.r, color_.g, color_.b, alpha_);
    }
}
