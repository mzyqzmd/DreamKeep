using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>奖励条目：面板按中立数据生成的一张"奖励卡"，只显示图标与一段文案，点击后交给数据里带来的回调。
    /// 点过即进入待处理（压暗、不可再点、不再响应悬停）：兑现时调 <see cref="Complete"/> 让自己从列表消失，
    /// 放弃时调 <see cref="Cancel"/> 解锁恢复——何时算兑现由外界决定，条目自己不认识奖励内容。
    /// 领取被拒时由外界调 <see cref="PlayRefuse"/> 让内层内容左右抖一下，条目状态与数据都不变。</summary>
    public sealed class UI_RewardItem : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>待处理时的不透明度：明显变暗但还能看清是什么奖励。</summary>
        private const float PendingAlpha = 0.35f;

        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _description;
        [Tooltip("抖动载体：内层 Content 容器，避开父级布局对根节点位置的重建")]
        [SerializeField] private RectTransform _content;
        [Tooltip("抖动幅度（画布像素）")] [SerializeField]
        private float _shakeAmplitudePx = 12f;
        [Tooltip("抖动时长（秒）")] [SerializeField] private float _shakeDuration = 0.3f;

        private Tween _shakeTween;
        private CanvasGroup _canvasGroup;
        private UI_HoverActive _hoverActive;
        private UI_HoverScaler _hoverScaler;
        private RewardEntry _entry;
        private Action<UI_RewardItem> _onCompleted;
        private bool _pending;
        private bool _completed;

        private void Awake()
        {
            // 预制体上没有 CanvasGroup：自愈补一个，待处理只需一个 alpha/射线开关，不必逐个组件写色
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            // 悬停表现由两个自驱组件负责：待处理期间关掉它们即可"既不再弹选框也不再放大"（两者 OnDisable 自带复位）
            _hoverActive = GetComponent<UI_HoverActive>();
            _hoverScaler = GetComponent<UI_HoverScaler>();
        }

        // 失活先停抖：在抖的先跳回基准再清（Settle），半途位移不会带进下次
        private void OnDisable() => UiTween.Settle(ref _shakeTween);

        #region 绑定与回执：待处理 → 完成移除 / 取消解锁 / 被拒抖动

        /// <summary>写入一条奖励的中立内容；<paramref name="onCompleted_"/> 是"我被兑现了，请把我还给面板"的回执通道。</summary>
        public void Bind(RewardEntry entry_, Action<UI_RewardItem> onCompleted_)
        {
            _entry = entry_;
            _onCompleted = onCompleted_;
            _completed = false;
            _icon.sprite = entry_.Icon;
            _description.text = entry_.Description ?? string.Empty;
            SetPending(false);
        }

        /// <summary>兑现完成：通知面板把自己从列表中移除（面板负责销毁实例）。</summary>
        public void Complete()
        {
            if (_completed) return;
            _completed = true;
            _onCompleted?.Invoke(this);
        }

        /// <summary>放弃本次领取：解锁回原样，玩家可以再点一次。</summary>
        public void Cancel()
        {
            if (_completed || !_pending) return;
            SetPending(false);
        }

        /// <summary>被拒反馈：内层内容左右抖一下，不改变条目状态与任何数据。</summary>
        public void PlayRefuse()
        {
            if (_completed) return;
            UiTween.PlayShake(ref _shakeTween, _content, _shakeAmplitudePx, _shakeDuration);
        }

        #endregion

        public void OnPointerClick(PointerEventData _)
        {
            if (_pending || _completed) return;
            SetPending(true);
            _entry.OnClaimed?.Invoke(this);
        }

        private void SetPending(bool pending_)
        {
            _pending = pending_;
            _canvasGroup.alpha = pending_ ? PendingAlpha : 1f;
            _canvasGroup.blocksRaycasts = !pending_;
            if (_hoverActive) _hoverActive.enabled = !pending_;
            if (_hoverScaler) _hoverScaler.enabled = !pending_;
        }
    }
}
