using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>
    /// 牌堆视图基类：收口抽牌堆 / 弃牌堆 / 消耗堆共用的计数文本与牌堆锚点，并统一"有牌进堆"的反馈——
    ///</summary>
    public abstract class UI_Pile : MonoBehaviour
    {
        [Tooltip("计数文本：显示本堆张数")] [SerializeField]
        protected TextMeshProUGUI _countText;

        [Header("进堆弹跳")] [Tooltip("弹跳幅度：0.08 即最多放大 8%")] [SerializeField]
        private float _popScale = 0.08f;

        [Tooltip("放大到峰值用的时长（秒）：极短，起来要快")] [SerializeField]
        private float _popUpDuration = 0.05f;

        [Tooltip("从当前缩放回落到基准缩放的时长（秒）：减速轻着地")] [SerializeField]
        private float _popDownDuration = 0.12f;

        protected abstract int LiveCount { get; } // 派生类给活值（表达式主体）

        private int _shown;
        private Vector3 _baseScale;
        private Tween _punchTween;

        private void Awake() => _baseScale = transform.localScale;
        private void OnDestroy() => _punchTween?.Kill();

        public void OnCardArrived() // 进堆一张：钳到活值上界 + 弹
        {
            _shown = Mathf.Min(_shown + 1, LiveCount);
            _countText.text = _shown.ToString();
            PlayPop();
        }

        public void OnCardLeft() // 离堆一张：钳到活值下界，不弹
        {
            _shown = Mathf.Max(_shown - 1, LiveCount);
            _countText.text = _shown.ToString();
        }

        public void SyncToLive() // 显示数直接对齐活值、不弹（构造初值 + 回合兜底共用）
        {
            _shown = LiveCount;
            _countText.text = _shown.ToString();
        }

        // 牌进堆：打断上一条弹跳后从当前缩放起弹——第一段不写起点，DOTween 自动取当前 localScale（即以当前缩放继续）；
        // 终点恒为本堆基准缩放，故洗回那种 0.04 秒一次的反复打断只会重置节奏，不会越弹越大
        protected void PlayPop()
        {
            _punchTween?.Kill();
            // TODO 进堆音效：牌进堆的反馈归本堆自己，将来在此处播放
            _punchTween = DOTween.Sequence()
                .Append(transform.DOScale(_baseScale * (1f + _popScale), _popUpDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(_baseScale, _popDownDuration).SetEase(Ease.OutQuad));
        }
    }
}