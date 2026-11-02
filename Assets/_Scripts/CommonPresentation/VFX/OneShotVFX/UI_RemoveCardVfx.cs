using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>移除卡牌演出：根下预摆一张展示卡，复刻"从牌库永久移除一张牌"的参考时序——
    /// 弹出（缩放 0→1，OutCubic）→ 停顿 → 竖向压扁到 0 且横向微张、同步整体渐黑 → 播完还池。
    /// 纯 transform 与 Graphic.color 驱动，无粒子无 shader；取池/摆位/到时还池沿 UI_OneShotVfx 契约，
    /// 动画只写展示卡子节点：根的缩放由工厂摆位写入，两者互不干扰。</summary>
    public sealed class UI_RemoveCardVfx : UI_OneShotVfx
    {
        [Header("展示卡")] [Tooltip("内嵌的被移除卡展示：由 Bind 写入本次移除的牌，全程显示、随压扁渐黑收场")] [SerializeField]
        private UI_CardDisplay _cardView;

        [Header("时序")] [SerializeField] private float _popDuration = 0.25f; // 弹出：缩放 0→1
        [SerializeField] private float _holdDuration = 1.5f; // 停顿：完整展示
        [SerializeField] private float _squashDuration = 0.4f; // 压扁渐黑段时长（原版未给，可调）
        [SerializeField] private float _squashStretchX = 1.5f; // 压扁时横向微张的目标倍数

        private Graphic[] _graphics; // 展示卡下全部图元（含 TMP 文本）：渐黑通道逐个乘色
        private Color[] _baseColors; // 各图元的原色：渐黑与复位都以它为基准
        private Tween _sequence;

        protected override void Awake()
        {
            base.Awake(); // 根不挂 Graphic 时 alpha 兜底自然跳过
            if (!_cardView) return;
            _graphics = _cardView.GetComponentsInChildren<Graphic>(true);
            _baseColors = new Color[_graphics.Length];
            for (int i = 0; i < _graphics.Length; i++) _baseColors[i] = _graphics[i].color;
        }

        protected override float Duration => _popDuration + _holdDuration + _squashDuration;

        protected override void PlayOnce()
        {
            ResetVisual();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_cardView.transform.DOScale(1f, _popDuration).SetEase(Ease.OutCubic));
            sequence.AppendInterval(_holdDuration);
            sequence.Append(_cardView.transform.DOScale(new Vector3(_squashStretchX, 0f, 1f), _squashDuration)
                .SetEase(Ease.Linear));
            // 渐黑与压扁同步：起点取自 getter（恒 0），只乘 rgb 保 alpha——Color.Lerp 会连 alpha 一起拉向 0
            sequence.Join(DOTween.To(() => 0f, WriteBlack, 1f, _squashDuration).SetEase(Ease.Linear));
            _sequence = sequence;
        }

        private void OnDisable() => Abort();

        private void OnDestroy() => _sequence?.Kill();

        #region 绑定：写入被移除的牌

        /// <summary>把本次被移除的牌写进展示卡；展示卡没接或卡为空时静默跳过，不影响调用方流程。</summary>
        public void Bind(CardModel card_)
        {
            if (_cardView && card_ != null) _cardView.Bind(card_);
        }

        #endregion

        private void ResetVisual()
        {
            RestoreColors();
            _cardView.transform.localScale = Vector3.zero;
        }

        // 还池/收段即中止：杀补间并复位展示卡，防实例复用串场
        private void Abort()
        {
            _sequence?.Kill();
            RestoreColors();
            _cardView.transform.localScale = Vector3.one;
        }

        private void WriteBlack(float progress_)
        {
            float k = 1f - progress_;
            for (int i = 0; i < _graphics.Length; i++)
            {
                Color color = _baseColors[i];
                color.r *= k;
                color.g *= k;
                color.b *= k;
                _graphics[i].color = color;
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < _graphics.Length; i++) _graphics[i].color = _baseColors[i];
        }
    }
}
