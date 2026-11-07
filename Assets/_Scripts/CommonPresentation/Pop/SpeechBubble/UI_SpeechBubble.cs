using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用对话气泡：一次只演一句台词，极短渐入（透明度 0→1）→ 停留 → 渐隐后自收。
    /// 播放期间再来请求直接拒绝（不排队、不打断），同侧不叠播的口径由调用方（PopFactory / 商店入口）复用本类的 IsPlaying 保证；
    /// 时间线自持，实例取还与父级挂载归调用方。预制体的朝向（L/R/商人）由预制体自身表达，本类不碰位姿。</summary>
    public sealed class UI_SpeechBubble : MonoBehaviour
    {
        [Header("基础配置")] [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _text;

        [Header("时间线")] [Tooltip("渐入时长（秒）：极短，只做一次透明度抬升")] [SerializeField]
        private float _fadeInDuration = 0.06f;

        [Tooltip("停留时长（秒）")] [SerializeField] private float _holdDuration = 1.5f;

        [Tooltip("渐隐时长（秒）")] [SerializeField] private float _fadeOutDuration = 0.25f;

        private bool _isPlaying;
        private Sequence _timeline;
        private Action _onFinished;

        /// <summary>是否正在播放：调用方据此丢弃同侧的新请求。</summary>
        public bool IsPlaying => _isPlaying;

        // 父级失活/池回收时兜底收尾：完成回调只放行一次，避免池二次归还
        private void OnDisable() => Finish();

        #region 对外入口：播放一句台词与还池复位

        /// <summary>播放一句台词：已在播放则拒绝并返回 false；播完自失活并回调一次 onFinished_（池归还用）。</summary>
        public bool Play(string text_, Action onFinished_ = null)
        {
            if (_isPlaying) return false;

            _isPlaying = true;
            _onFinished = onFinished_;
            _text.text = text_;
            gameObject.SetActive(true);

            _timeline?.Kill();
            _canvasGroup.alpha = 0f;
            _timeline = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _fadeInDuration))
                .AppendInterval(_holdDuration)
                .Append(_canvasGroup.DOFade(0f, _fadeOutDuration))
                .OnComplete(Finish);
            return true;
        }

        /// <summary>还池复位：杀时间线、清回调与播放标记；下一次 Play 会重写全部状态。</summary>
        public void ResetView()
        {
            _timeline?.Kill();
            _timeline = null;
            _isPlaying = false;
            _onFinished = null;
        }

        #endregion

        // 收尾只放行一次：OnComplete 与 OnDisable 都会走到这里
        private void Finish()
        {
            if (!_isPlaying) return;

            _isPlaying = false;
            _timeline?.Kill();
            _timeline = null;
            Action finished = _onFinished;
            _onFinished = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
            finished?.Invoke();
        }
    }
}
