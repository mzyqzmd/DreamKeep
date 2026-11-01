using System;
using _DreamKeep.CommonPresentation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using TMPro;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物槽位：显示一件遗物的图标与悬停说明，并在该遗物触发效果时播一次"图标变亮 + 轮廓涟漪"；
    /// 点击图标弹出该遗物的信息面板。常态即着色器参数归零、触发中即补间在跑，重播就是先杀旧补间再从头开始。</summary>
    public sealed class UI_RelicSlot : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("图标层：显示遗物图标，触发时由材质的 _Boost 提亮")] [SerializeField]
        private Image _icon;

        [Tooltip("涟漪层：比图标更大的透明层，由着色器按图标轮廓算出向外扩散的环")] [SerializeField]
        private Image _ripple;

        [Tooltip("节奏型遗物右下角的计数数字：收到首条计数通知才显示，非节奏型遗物恒隐藏")] [SerializeField]
        private TextMeshProUGUI _number;

        [Tooltip("两层共用的遗物着色器")] [SerializeField]
        private Shader _relicShader;

        [Tooltip("悬停说明的气泡发起器：生成槽位时写入遗物名与描述")] [SerializeField]
        private UI_HoverTip _hoverTip;

        [Header("用尽表现")] [Tooltip("遗物用尽后图标定格的颜色")] [SerializeField]
        private Color _exhaustedColor = new(0.35f, 0.35f, 0.35f, 1f);

        // 用尽后补在说明末尾的一行：Icon 变灰只能表示"看起来不对"，还得有一句话讲清它不再生效；
        // 悬停气泡与信息面板共用这一句，故对外公开
        public const string ExhaustedTip = "这个遗物的效果已经耗尽";

        [Header("触发表现")] [Tooltip("图标提亮的亮度倍率")] [SerializeField]
        private float _boostScale = 1.6f;

        [Tooltip("亮起时长")] [SerializeField] private float _boostUpDuration = 0.08f;

        [Tooltip("亮着不动的保持时长")] [SerializeField]
        private float _boostHoldDuration = 0.1f;

        [Tooltip("回落时长")] [SerializeField] private float _boostDownDuration = 0.22f;

        [Tooltip("涟漪从轮廓扩散到消失的总时长")] [SerializeField]
        private float _rippleDuration = 0.45f;

        [Header("涟漪外观")] [Tooltip("涟漪颜色，A 通道即环的透明度")] [SerializeField]
        private Color _rippleColor = Color.white;

        [Tooltip("环的粗细（按涟漪层归一化）")] [Range(0.01f, 0.4f)] [SerializeField]
        private float _rippleThickness = 0.08f;

        [Tooltip("环从图标轮廓向外扩散的倍率，超出涟漪层画布的部分会被裁掉")] [Range(1f, 1.5f)] [SerializeField]
        private float _rippleExpand = 1.5f;

        private static readonly int BoostId = Shader.PropertyToID("_Boost");
        private static readonly int BaseAlphaId = Shader.PropertyToID("_BaseAlpha");
        private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
        private static readonly int RippleColorId = Shader.PropertyToID("_RippleColor");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int ExpandId = Shader.PropertyToID("_Expand");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int IconTexId = Shader.PropertyToID("_IconTex");
        private static readonly int IconRectId = Shader.PropertyToID("_IconRect");
        private static readonly int IconScaleId = Shader.PropertyToID("_IconScale");

        private Material _iconMaterial; // 一槽一份：提亮逐槽独立
        private Material _rippleMaterial; // 一槽一份：图标贴图与扩散进度逐槽独立
        private Tween _boostTween;
        private Tween _progressTween;
        private SO_Relic _relic;
        private bool _exhausted; // 本槽位这件遗物是否已用尽：点击弹面板时随遗物一起交出去

        /// <summary>点击播报：把本槽位这件遗物与"是否已用尽"交给订阅者（遗物栏）。
        /// 槽位不注入遗物栏——遗物栏在自己注入期经池造槽位，槽位若反向 Resolve 遗物栏，
        /// 会撞上容器构建期的单例重入（Lazy 工厂未跑完就被再入），故点击只广播、路由归遗物栏。</summary>
        public event Action<SO_Relic, bool> Clicked;

        private void Awake()
        {
            EnsureMaterials();
            ResetVisual();
        }

        // 材质按槽独占：Awake 创建一次即可；但池化/注入时序可能让 Bind 早于 Awake 执行，
        // 故 Bind 也兜底确保材质已就位，避免 _iconMaterial 为空触发空引用
        private void EnsureMaterials()
        {
            if (_iconMaterial == null) _iconMaterial = new Material(_relicShader);
            if (_rippleMaterial == null) _rippleMaterial = new Material(_relicShader);
            _iconMaterial.SetFloat(BaseAlphaId, 1f); // 图标层画 sprite
            _rippleMaterial.SetFloat(BaseAlphaId, 0f); // 涟漪层不画 sprite，只由着色器算环
            // 外观是预制体级常量：造材质时写一次即可，池化复用不重写
            _rippleMaterial.SetColor(RippleColorId, _rippleColor);
            _rippleMaterial.SetFloat(ThicknessId, _rippleThickness);
            _rippleMaterial.SetFloat(ExpandId, _rippleExpand);
            _icon.material = _iconMaterial;
            _ripple.material = _rippleMaterial;
        }

        // 还池与关激活时立刻收尾：补间被 Kill 不会走完成回调，亮度与涟漪必须在这里归零
        private void OnDisable() => ResetVisual();

        private void OnDestroy()
        {
            _boostTween?.Kill();
            _progressTween?.Kill();
            if (_iconMaterial) Destroy(_iconMaterial);
            if (_rippleMaterial) Destroy(_rippleMaterial);
        }

        #region 绑定遗物：图标、悬停说明与轮廓采样参数

        public void Bind(SO_Relic relic_)
        {
            EnsureMaterials();
            _number.gameObject.SetActive(false); // 默认隐藏：非节奏型遗物永不收到计数通知、恒不显示
            _relic = relic_;
            _icon.sprite = relic_.Icon;
            _hoverTip.SetContents(_relic.RelicName, _relic.Description);
            Sprite icon = relic_.Icon;
            if (!icon)
            {
                // 无图标遗物：图标层留空，涟漪也没有轮廓可依
                _iconMaterial.SetFloat(BaseAlphaId, 0f);
                return;
            }

            _iconMaterial.SetFloat(BaseAlphaId, 1f);
            Texture texture = icon.texture;
            Rect rect = icon.textureRect;
            _rippleMaterial.SetTexture(IconTexId, texture);
            _rippleMaterial.SetVector(IconRectId, new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height));
            // 图标在涟漪层里占的尺寸比例：涟漪按图标轮廓扩散，必须用两者画出来的宽度比（涟漪层自身带缩放）
            RectTransform iconRect = _icon.rectTransform;
            RectTransform rippleRect = _ripple.rectTransform;
            _rippleMaterial.SetFloat(IconScaleId, Mathf.Abs(iconRect.rect.width * iconRect.lossyScale.x
                                                            / (rippleRect.rect.width * rippleRect.lossyScale.x)));
        }

        #endregion

        // 点击图标：把本槽位这件遗物与"是否已用尽"反向播报给遗物栏，由它弹信息面板
        public void OnPointerClick(PointerEventData eventData_) => Clicked?.Invoke(_relic, _exhausted);

        #region 触发表现：图标提亮一次、涟漪扩散一次，播完自动回常态

        public void PlayTrigger()
        {
            _rippleMaterial.SetFloat(RippleStrengthId, 1f);
            UiTween.PlayScalar(ref _progressTween, () => 0f, WriteProgress, 1f, _rippleDuration, Ease.Linear)
                .OnComplete(ResetRipple);

            _boostTween?.Kill();
            _boostTween = DOTween.Sequence()
                .Append(DOTween.To(() => 1f, WriteBoost, _boostScale, _boostUpDuration).SetEase(Ease.OutQuad))
                .AppendInterval(_boostHoldDuration)
                .Append(DOTween.To(() => _boostScale, WriteBoost, 1f, _boostDownDuration).SetEase(Ease.OutQuad));
        }

        private void WriteProgress(float progress_) => _rippleMaterial.SetFloat(ProgressId, progress_);

        private void WriteBoost(float boost_) => _iconMaterial.SetFloat(BoostId, boost_);

        // 环收尾：进度归零、强度关掉（下一次触发从轮廓重新长出来）
        private void ResetRipple()
        {
            _rippleMaterial.SetFloat(ProgressId, 0f);
            _rippleMaterial.SetFloat(RippleStrengthId, 0f);
        }

        #endregion

        // 复位到常态：杀两条补间并把亮度、进度、涟漪强度写回默认；颜色一并复位，还池后不留灰
        private void ResetVisual()
        {
            _boostTween?.Kill();
            _progressTween?.Kill();
            WriteBoost(1f);
            ResetRipple();
            SetExhausted(false);
        }

        /// <summary>用尽后把图标定格为灰、说明末尾补一行"已耗尽"；复位时恢复本色与原说明。</summary>
        public void SetExhausted(bool exhausted_)
        {
            _exhausted = exhausted_;
            _icon.color = exhausted_ ? _exhaustedColor : Color.white;
            if (_relic == null) return; // Awake 的复位早于 Bind，此时没有说明可写
            _hoverTip.SetContents(_relic.RelicName,
                exhausted_ ? _relic.Description + "\n" + ExhaustedTip : _relic.Description);
        }

        #region 节奏计数：右下角数字随遗物效果实时刷新

        /// <summary>刷新节奏型遗物的当前计数值；首条通知即点亮数字，非节奏型遗物永不调用故数字恒隐藏。</summary>
        public void SetCount(int count_)
        {
            _number.gameObject.SetActive(true);
            _number.text = count_.ToString();
        }

        #endregion
    }
}