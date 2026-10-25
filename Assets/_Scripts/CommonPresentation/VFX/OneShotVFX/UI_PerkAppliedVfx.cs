using DG.Tweening;
using TMPro;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>施加状态演出：状态大图标淡入放大、身后一层加色残影、名称从图标上方继续上浮，增益染绿、减益染红。
    /// 继承单次特效播放器，只把"怎么播"换成一条进度补间；内容由 Bind 喂入，摆位与取还池仍走基类契约。
    /// 根节点刻意不挂 Graphic：基类的 alpha 兜底只认根，挂了会与这里的补间打架。</summary>
    public sealed class UI_PerkAppliedVfx : UI_OneShotVfx
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int BoostId = Shader.PropertyToID("_Boost");

        [Header("内容")] [SerializeField] private RectTransform _content;
        [SerializeField] private Image _icon;
        [SerializeField] private CanvasGroup _iconGroup;
        [SerializeField] private Image _echo;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private CanvasGroup _nameGroup;

        [Header("图标")] [SerializeField] private float _iconFromScale = 0.4f;
        [SerializeField] private float _iconToScale = 0.8f;
        [SerializeField] private float _iconAlphaPeak = 0.5f;

        [Tooltip("整体相对锚点的上抬量（画布 px）")] [SerializeField]
        private float _liftPx = 60f;

        [Header("残影")] [SerializeField] private float _echoToScale = 1.3f;
        [SerializeField] private float _echoAlphaPeak = 0.6f;

        [Tooltip("残影材质的加色强度：Image.color 是 Color32 提不亮，亮度只能走材质")] [SerializeField]
        private float _echoBoost = 1.5f;

        [Header("名称")] [Tooltip("名称起点相对图标中心的高度（画布 px）")] [SerializeField]
        private float _nameOffsetPx = 130f;

        [SerializeField] private float _nameLiftPx = 50f;

        [Tooltip("名称淡入占整段的比例")] [SerializeField]
        private float _nameFadeIn = 0.15f;

        [Tooltip("名称开始淡出的进度点")] [SerializeField]
        private float _nameFadeOutStart = 0.6f;

        [SerializeField] private Color _buffColor = new(0.35f, 0.9f, 0.35f);
        [SerializeField] private Color _debuffColor = new(0.95f, 0.32f, 0.32f);

        [Header("时序")] [Tooltip("整段时长（秒）")] [SerializeField]
        private float _duration = 1.25f;

        [Tooltip("同目标同时演多份时，第 2 份起的水平间隔（画布 px）")] [SerializeField]
        private float _spreadPx = 110f;

        private RectTransform _iconRect;
        private RectTransform _echoRect;
        private RectTransform _nameRect;
        private Material _echoMaterial;
        private Tween _progressTween;
        private float _progress;

        protected override void Awake()
        {
            base.Awake();
            _iconRect = (RectTransform)_icon.transform;
            _echoRect = (RectTransform)_echo.transform;
            _nameRect = (RectTransform)_name.transform;
            // 残影的亮度与透明度都改材质，故每实例克隆一份：共享资产上写值会串到别的实例
            _echoMaterial = new Material(_echo.material);
            _echo.material = _echoMaterial;
            _echoMaterial.SetFloat(BoostId, _echoBoost);
        }

        private void OnDestroy()
        {
            _progressTween?.Kill();
            if (_echoMaterial) Destroy(_echoMaterial);
        }

        #region 绑定内容：图标、名称、极性颜色与水平错开

        /// <summary>写本次要演的内容：图标与名称取自状态运行时实例，名称按极性上色，
        /// 并按同父节点下仍在播的同类型实例数把整体水平错开。</summary>
        public void Bind(BattlePerk perk_)
        {
            Sprite icon = perk_.Icon;
            _icon.sprite = icon;
            _echo.sprite = icon;
            _name.text = perk_.Name;
            _name.color = perk_.Polarity == EPerkPolarity.Debuff ? _debuffColor : _buffColor;
            _nameRect.anchoredPosition = new Vector2(0f, _nameOffsetPx);
            _content.anchoredPosition = new Vector2(SpreadOf(ActiveSiblingIndex()), _liftPx);
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

        // 进度 → 全部通道：图标的放大走 OutExpo 解析式（DOTween 的 OutExpo 即 1 - 2^(-10t)），其余通道保持线性
        private void ApplyProgress(float progress_)
        {
            float eased = progress_ >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * progress_);
            _iconRect.localScale = Vector3.one * Mathf.LerpUnclamped(_iconFromScale, _iconToScale, eased);
            _iconGroup.alpha = Mathf.Sin(Mathf.PI * progress_) * _iconAlphaPeak;

            _echoRect.localScale = Vector3.one * Mathf.LerpUnclamped(1f, _echoToScale, progress_);
            _echoMaterial.SetColor(TintId, new Color(1f, 1f, 1f, (1f - progress_) * _echoAlphaPeak));

            float fadeOut = Mathf.Clamp01((progress_ - _nameFadeOutStart) / Mathf.Max(1f - _nameFadeOutStart, 1e-4f));
            _nameGroup.alpha = Mathf.Clamp01(progress_ / Mathf.Max(_nameFadeIn, 1e-4f)) * (1f - fadeOut);
            _nameRect.anchoredPosition = new Vector2(0f, _nameOffsetPx + _nameLiftPx * progress_);
        }

        // 在播序号：同父节点下仍在用的同类型实例数。空闲实例归还时已被挂回工厂根下，天然不计入
        private int ActiveSiblingIndex()
        {
            Transform parent = transform.parent;
            if (!parent) return 0;
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != transform && child.gameObject.activeSelf && child.GetComponent<UI_PerkAppliedVfx>())
                    count++;
            }

            return count;
        }

        // 错开序号 → 水平位移：0 / +1 / -1 / +2 / -2，偶数往右、奇数往左
        private float SpreadOf(int index_)
        {
            int step = (index_ + 1) / 2;
            return _spreadPx * (index_ % 2 == 1 ? step : -step);
        }
    }
}
