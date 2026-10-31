using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>单件遗物选项：图标/名称/描述来自 SO_Relic，点击只上报不自行动（继承自 UI_Button）；淡入演出供组错峰调度。</summary>
    public sealed class UI_AncientOptionItem : UI_Button
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;

        [SerializeField] private float _fadeDuration = 0.2f;

        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;

        private void Awake()
        {
            // CanvasGroup 自愈：预制体没挂也能淡入（照回合横幅先例）
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
        }

        protected override void OnDestroy() => _fadeTween?.Kill();

        public void Bind(SO_Relic relic_)
        {
            gameObject.SetActive(true); // 激活即同步触发 Awake，保证 _canvasGroup 就绪
            _icon.gameObject.SetActive(relic_.Icon != null);
            if (relic_.Icon != null) _icon.sprite = relic_.Icon;
            _title.text = relic_.RelicName;
            _description.text = relic_.Description;
        }

        /// <summary>错峰淡入：delay_ 秒后从透明淡到全显，重复调用杀旧重播。</summary>
        public void PlayFadeIn(float delay_)
        {
            _canvasGroup.alpha = 0f; // 从零起播
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, 1f, _fadeDuration, delay_);
        }

        public void Hide()
        {
            _fadeTween?.Kill();
            gameObject.SetActive(false);
        }
    }
}
