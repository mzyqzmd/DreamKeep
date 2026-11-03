using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>单件遗物选项：图标/名称/描述来自 SO_Relic，点击只上报不自行动（继承自 UI_Button）；淡入演出供组错峰调度。
    /// 生命值不足以支付拾取代价时置灰：整件变暗、基类 Enabled 吞点击、描述末尾补一行原因，仍占位显示不隐藏。</summary>
    public sealed class UI_AncientOptionItem : UI_Button
    {
        private const float DisabledAlpha = 0.4f;
        private const string InsufficientHpTip = "生命值不足";

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;

        [SerializeField] private float _fadeDuration = 0.2f;

        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;
        private string _descriptionText; // 本轮绑定的原始描述：置灰时在其后补原因，恢复时原样写回

        private void Awake()
        {
            // CanvasGroup 自愈：预制体没挂也能淡入（照回合横幅先例）
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
        }

        protected override void OnDestroy() => KillFade();

        public void Bind(SO_Relic relic_, bool claimable_)
        {
            gameObject.SetActive(true); // 激活即同步触发 Awake，保证 _canvasGroup 就绪
            _icon.gameObject.SetActive(relic_.Icon != null);
            if (relic_.Icon != null) _icon.sprite = relic_.Icon;
            _title.text = relic_.RelicName;
            _descriptionText = relic_.Description;
            SetEnabled(claimable_);
        }

        /// <summary>错峰淡入：delay_ 秒后从透明淡到目标透明度（置灰项淡到更暗），重复调用杀旧重播。</summary>
        public void PlayFadeIn(float delay_)
        {
            _canvasGroup.alpha = 0f; // 从零起播
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, Enabled ? 1f : DisabledAlpha, _fadeDuration, delay_)
                .OnComplete(() => _fadeTween = null); // 播完即弃引用：Tween 会被回收复用，留着再 Kill 会误杀别人
        }

        public void Hide()
        {
            KillFade();
            gameObject.SetActive(false);
        }

        // 置灰/恢复：禁用点击（基类 Enabled 接管点击）并把整件调到半透明，置灰时描述末尾补一行原因。
        // 先打断淡入补间——补间目标在启动时按当时的 Enabled 算定，迟到的写入会把透明度覆盖回 1.0
        private void SetEnabled(bool enabled_)
        {
            KillFade();
            Enabled = enabled_;
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _canvasGroup.alpha = enabled_ ? 1f : DisabledAlpha;
            _description.text = enabled_ ? _descriptionText : _descriptionText + "\n" + InsufficientHpTip;
        }

        // 打断淡入补间并清空引用：Kill 后 DOTween 会把该 Tween 回收给他人复用，残留引用再 Kill 会误杀别的补间
        private void KillFade()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }
    }
}
