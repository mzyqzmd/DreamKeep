using DG.Tweening;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>药水落地演出：飞溅一层在落点炸开后从边缘侵蚀消散，贴地液体一层延迟出场、撑开、横向淌开并淡出。
    /// 继承单次特效播放器，只把"怎么播"换成一条进度补间；液体颜色由 Bind 喂入，摆位与取还池仍走基类契约。
    /// 根节点刻意不挂 Graphic：基类的 alpha 兜底只认根，挂了会与这里的补间打架。</summary>
    public sealed class UI_PotionSplashVfx : UI_OneShotVfx
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        [Header("两层视图")] [SerializeField] private Image _splash;
        [SerializeField] private Image _liquid;

        [Header("飞溅")] [SerializeField] private float _splashFromScale = 0.55f;
        [SerializeField] private float _splashToScale = 1.2f;

        [Tooltip("冲到峰值尺寸的时长（秒）")] [SerializeField]
        private float _splashGrowSeconds = 0.12f;

        [Tooltip("侵蚀走完的时长（秒）：到点整张图已被吃干净")] [SerializeField]
        private float _splashErodeSeconds = 0.45f;

        [Header("液体")] [Tooltip("相对落地的延迟出场（秒）")] [SerializeField]
        private float _liquidStartSeconds = 0.08f;

        [Tooltip("从起始尺寸撑到峰值的时长（秒）")] [SerializeField]
        private float _liquidGrowSeconds = 0.25f;

        [SerializeField] private float _liquidFromScale = 0.6f;
        [SerializeField] private float _liquidToScale = 1f;

        [Tooltip("撑满之后再横向拉宽的倍率")] [SerializeField]
        private float _liquidStretch = 1.15f;

        [Header("整体")] [Tooltip("整段时长（秒）")] [SerializeField]
        private float _duration = 0.6f;

        [Tooltip("淡出起点（秒）：两层都在这一段里收干净")] [SerializeField]
        private float _fadeOutStartSeconds = 0.45f;

        [SerializeField] private float _liquidFadeInSeconds = 0.06f;
        [SerializeField] private float _liquidAlphaPeak = 0.85f;

        private RectTransform _splashRect;
        private RectTransform _liquidRect;
        private Vector3 _liquidBaseScale;
        private Material _splashMaterial;
        private Material _liquidMaterial;
        private Tween _progressTween;
        private float _progress;

        protected override void Awake()
        {
            base.Awake();
            _splashRect = (RectTransform)_splash.transform;
            _liquidRect = (RectTransform)_liquid.transform;
            _liquidBaseScale = _liquidRect.localScale;
            // 两层各克隆一份材质：进度与色相都是每实例通道，共享资产上写值会串到别的实例
            _splashMaterial = new Material(_splash.material);
            _splash.material = _splashMaterial;
            _liquidMaterial = new Material(_liquid.material);
            _liquid.material = _liquidMaterial;
            ApplyProgress(0f);
            // 取池到开播之间不该露脸：进 Play 时 ApplyProgress 会把它写回不透明
            _splashMaterial.SetFloat(AlphaId, 0f);
        }

        private void OnDestroy()
        {
            _progressTween?.Kill();
            if (_splashMaterial) Destroy(_splashMaterial);
            if (_liquidMaterial) Destroy(_liquidMaterial);
        }

        #region 绑定内容：液体颜色

        /// <summary>写本次的液体色：两层共用一个颜色，着色在 shader 里按"去色再乘色"处理。</summary>
        public void Bind(Color color_)
        {
            _splashMaterial.SetColor(TintId, color_);
            _liquidMaterial.SetColor(TintId, color_);
        }

        #endregion

        protected override void PlayOnce()
        {
            _progress = 0f;
            ApplyProgress(0f);
            UiTween.PlayScalar(ref _progressTween, () => _progress, value_ => ApplyProgress(_progress = value_),
                1f, _duration, Ease.Linear);
        }

        protected override float Duration => _duration;

        // 进度 → 两层通道：飞溅只管"冲尺寸 + 侵蚀"，液体管"延迟出场 + 撑开 + 拉宽 + 淡出"，两层错开才看得出先溅后淌
        private void ApplyProgress(float progress_)
        {
            float seconds = progress_ * _duration;

            float grow = OutExpo(seconds / _splashGrowSeconds);
            _splashRect.localScale = Vector3.one * Mathf.LerpUnclamped(_splashFromScale, _splashToScale, grow);
            _splashMaterial.SetFloat(ProgressId, Mathf.Clamp01(seconds / _splashErodeSeconds));
            _splashMaterial.SetFloat(AlphaId, 1f);

            float liquidGrow = OutExpo((seconds - _liquidStartSeconds) / _liquidGrowSeconds);
            float stretch = Mathf.Clamp01(
                (seconds - _liquidStartSeconds - _liquidGrowSeconds) / Mathf.Max(_fadeOutStartSeconds - _liquidStartSeconds - _liquidGrowSeconds, 1e-4f));
            float scale = Mathf.LerpUnclamped(_liquidFromScale, _liquidToScale, liquidGrow);
            _liquidRect.localScale = new Vector3(
                _liquidBaseScale.x * scale * Mathf.LerpUnclamped(1f, _liquidStretch, stretch),
                _liquidBaseScale.y * scale, _liquidBaseScale.z);
            _liquidMaterial.SetFloat(AlphaId, _liquidAlphaPeak
                                              * Mathf.Clamp01((seconds - _liquidStartSeconds) / _liquidFadeInSeconds)
                                              * (1f - FadeOutOf(seconds)));
        }

        // 淡出比例：0 表示还没到淡出点，1 表示整段结束
        private float FadeOutOf(float seconds_) =>
            Mathf.Clamp01((seconds_ - _fadeOutStartSeconds) / Mathf.Max(_duration - _fadeOutStartSeconds, 1e-4f));

        // OutExpo：DOTween 的同名缓动就是 1 - 2^(-10t)，这里要按秒切片所以自己算，负值（还没出场）归零
        private static float OutExpo(float t_) =>
            t_ >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * Mathf.Clamp01(t_));
    }
}
