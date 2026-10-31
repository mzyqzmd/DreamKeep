using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>一条对话气泡：Bind 写文本并应用说话人外观（头像/底色/字色），PlayRevealAsync 淡入加逐字揭示，
    /// 点击补完由 CompleteReveal 置位；整条气泡由面板按行生成与销毁。</summary>
    public sealed class UI_AncientDialogueItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _portrait;
        [SerializeField] private Image _bubbleBackground;
        [SerializeField] private Image _bubbleBackgroundTail;

        [SerializeField] private float _fadeDuration = 0.15f;

        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;
        private bool _revealSkipped;

        private void Awake()
        {
            // CanvasGroup 自愈：预制体没挂也能淡入（照回合横幅先例）
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
        }

        private void OnDestroy() => _fadeTween?.Kill();

        public void Bind(string text_, AncientSpeakerView view_)
        {
            gameObject.SetActive(true); // 激活即同步触发 Awake，保证 _canvasGroup 就绪
            _fadeTween?.Kill();
            _canvasGroup.alpha = 1f;
            _text.text = text_;
            _text.color = view_.TextColor;
            _bubbleBackground.color = view_.BubbleColor;
            _bubbleBackgroundTail.color = view_.BubbleColor;
            _portrait.gameObject.SetActive(view_.Portrait != null);
            if (view_.Portrait != null) _portrait.sprite = view_.Portrait;
            _revealSkipped = false;
        }

        /// <summary>淡入并逐字揭示：等待在"全部揭示 / 被补完 / 宿主销毁"中最早者返回。</summary>
        public async UniTask PlayRevealAsync(float charsPerSecond_, CancellationToken ct_)
        {
            _canvasGroup.alpha = 0f; // 从零起播
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, 1f, _fadeDuration);

            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate();
            int total = _text.textInfo.characterCount;
            float interval = 1f / Mathf.Max(1f, charsPerSecond_);
            for (int shown = 0; shown < total && !_revealSkipped; shown++)
            {
                _text.maxVisibleCharacters = shown + 1;
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct_);
            }

            _text.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>补完揭示：把逐字等待短路成整句立即可见。</summary>
        public void CompleteReveal() => _revealSkipped = true;
    }
}
