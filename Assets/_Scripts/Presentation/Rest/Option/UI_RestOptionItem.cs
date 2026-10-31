using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>单个休息选项：复用场景槽位已有的点击上报（继承自 UI_Button），悬停进出只广播事件；错峰淡入交组调度。</summary>
    public sealed class UI_RestOptionItem : UI_Button, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _fadeDuration = 0.2f;

        [Tooltip("选项图标：按选项语义换图（休息/锻造/挖掘各自的贴图），空引用时隐藏")]
        [SerializeField]
        private Image _icon;

        private const float DisabledAlpha = 0.4f;

        /// <summary>悬停进入：组据此显示本槽文案。</summary>
        public event Action Hovered;

        /// <summary>悬停离开：组据此隐藏文案。</summary>
        public event Action Unhovered;

        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;

        private void Awake() => _canvasGroup = UiCanvas.EnsureCanvasGroup(this);

        protected override void OnDestroy() => _fadeTween?.Kill();

        /// <summary>错峰淡入：delay_ 秒后从透明淡到目标透明度（禁用态更暗表示置灰），重复调用杀旧重播。</summary>
        public void PlayFadeIn(float delay_)
        {
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _canvasGroup.alpha = 0f; // 从零起播
            float target = Enabled ? 1f : DisabledAlpha;
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, target, _fadeDuration, delay_);
        }

        /// <summary>按选项语义换图标：空引用时隐藏图位。</summary>
        public void SetIcon(Sprite icon_)
        {
            if (!_icon) return;
            _icon.sprite = icon_;
            _icon.gameObject.SetActive(icon_ != null);
        }

        /// <summary>置灰：禁用点击（基类 Enabled 接管）并把整件调到半透明；悬停文案照常显示。</summary>
        public void SetEnabled(bool enabled_)
        {
            Enabled = enabled_;
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _canvasGroup.alpha = enabled_ ? 1f : DisabledAlpha;
        }

        public void OnPointerEnter(PointerEventData eventData_) => Hovered?.Invoke();

        public void OnPointerExit(PointerEventData eventData_) => Unhovered?.Invoke();
    }
}
