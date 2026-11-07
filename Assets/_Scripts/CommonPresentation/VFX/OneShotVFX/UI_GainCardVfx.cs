using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>获得卡牌演出：根下预摆一张展示卡——满尺寸直出、完整展示一拍后整卡缩小渐隐飞向目标点，
    /// 表达「这张卡已收入牌库」。纯 transform 与 CanvasGroup.alpha 驱动，无粒子无 shader；
    /// 取池/摆位/到时还池沿 UI_OneShotVfx 契约：位移写根（工厂摆位后根自由）、缩放写展示卡、alpha 写 CanvasGroup。</summary>
    public sealed class UI_GainCardVfx : UI_OneShotVfx
    {
        [Header("展示卡")] [Tooltip("内嵌的获得卡展示：由 Bind 写入本次获得的牌，全程显示、随飞行渐隐收场")] [SerializeField]
        private UI_CardDisplay _cardView;

        [Header("时序")] [SerializeField] private float _holdDuration = 1f; // 展示：满尺寸停留
        [SerializeField] private float _flyDuration = 0.5f; // 飞行：位移+缩小+渐隐并行
        [SerializeField] private float _flyScale = 0.2f; // 飞行终点缩放：展示卡原尺寸的倍数
        [SerializeField] private Ease _flyEase = Ease.InQuad; // 飞行缓动：加速离场感

        private Tween _sequence;
        private Vector3 _targetPosition; // 飞行落点（世界坐标）：每次播放前由编排经工厂设置

        protected override void Awake()
        {
            base.Awake(); // 根不挂 Graphic 时 alpha 兜底自然跳过
            // 展示卡不拦射线也不弹气泡：演出悬在屏幕中心，射线全关（UI_TargetArrow 同款先例）
            if (!_cardView) return;
            foreach (Graphic graphic in _cardView.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        protected override float Duration => _holdDuration + _flyDuration;

        protected override void PlayOnce()
        {
            transform.localScale = Vector3.one;
            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(_holdDuration); // 完整展示一拍
            // 三通道并行：根位移直达落点、展示卡缩到终点倍数、整卡渐隐
            sequence.Append(transform.DOScale(_flyScale, _flyDuration).SetEase(_flyEase));
            sequence.Join(transform.DOMove(_targetPosition, _flyDuration).SetEase(_flyEase));
            _sequence = sequence;
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            transform.localScale = Vector3.one;
        }

        private void OnDestroy() => _sequence?.Kill();

        #region 绑定与落点：写入本次获得的牌与飞行终点

        /// <summary>把本次获得的牌写进展示卡；展示卡没接或卡为空时静默跳过，不影响调用方流程。</summary>
        public void Bind(CardModel card_)
        {
            if (_cardView && card_ != null) _cardView.Bind(card_);
        }

        /// <summary>设置飞行落点（世界坐标）：每次播放前由编排经工厂回调写入。</summary>
        public void SetTarget(Vector3 targetWorldPosition_) => _targetPosition = targetWorldPosition_;

        #endregion
    }
}