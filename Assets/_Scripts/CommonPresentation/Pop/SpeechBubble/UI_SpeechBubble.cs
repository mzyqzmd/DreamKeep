using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用对话气泡：一次只演一句台词，极短渐入（透明度 0→1）→ 停留 → 渐隐后自收。
    /// 每次播放自带「是否允许被打断」：播放中再来请求时，只有当前气泡允许被打断才杀旧时间线原地重播，
    /// 否则直接拒绝（不排队、不叠播）；同侧唯一与实例取还归调用方（PopFactory / 商店入口）。
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
        private bool _allowInterrupt;
        private Sequence _timeline;
        private Action _onFinished;

        // 父级失活/池回收时兜底收尾：完成回调只放行一次，避免池二次归还
        private void OnDisable() => Finish();

        /// <summary>是否正在播放：调用方据此判定同侧是否已有气泡。</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>当前这条气泡是否允许被新台词顶掉：只有播放中且本次播放标记为可打断才为真。</summary>
        public bool CanBeInterrupted => _isPlaying && _allowInterrupt;

        #region 对外入口：播放一句台词与还池复位

        /// <summary>播放一句台词：播放中且当前气泡不允许被打断时拒绝并返回 false；允许打断则杀旧时间线原地重播。
        /// 播完自失活并回调一次 onFinished_（池归还用），打断重播时旧回调被新回调覆盖，不二次归还。</summary>
        public bool Play(string line_, bool allowInterrupt_, Action onFinished_ = null)
        {
            if (_isPlaying && !_allowInterrupt) return false;

            _timeline?.Kill();
            _isPlaying = true;
            _allowInterrupt = allowInterrupt_;
            _onFinished = onFinished_;
            _text.text = line_;
            gameObject.SetActive(true);

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
            _allowInterrupt = false;
            _onFinished = null;
        }

        #endregion

        // 收尾只放行一次：OnComplete 与 OnDisable 都会走到这里；先存回调，ResetView 会清掉它
        private void Finish()
        {
            if (!_isPlaying) return;

            Action finished = _onFinished;
            ResetView();
            if (gameObject.activeSelf) gameObject.SetActive(false);
            finished?.Invoke();
        }
    }
}
