using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>悬停缩放执行器：一条补间、两种触发。自驱模式（默认，挂到任意对象上即生效）自己接指针事件，
    /// 进入按"进入时的基准 × 倍数"放大、离开回到基准，失活即复位；被驱动模式关掉 _selfDriven，
    /// 由宿主在裁决出悬停的那一刻调 PlayHoverScale / PlayScale，把缩放补到给定值。
    /// 等比缩放（写入 Vector3.one × 值）；同通道独占由 UiTween 保证（建前先杀旧值）；
    /// 通道交接与回收由宿主调 Kill，本类不猜宿主状态。</summary>
    public sealed class UI_HoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("自驱模式：开＝自己接指针事件（挂到按钮等对象上即生效）；关＝由宿主调 PlayHoverScale / PlayScale 驱动")] [SerializeField]
        private bool _selfDriven = true;

        [Tooltip("自驱模式的放大倍数：进入时的缩放 × 该值（被驱动模式不用它）")] [SerializeField]
        private float _scaleFactor = 1.2f;

        [Tooltip("悬停目标缩放（被驱动模式用）：1 为常态，卡牌这类尺寸统一的对象直接写目标绝对值")] [SerializeField]
        private float _hoverScale = 1.05f;

        [Tooltip("补间时长（秒）：宿主的其它演出要与它对齐时读 Duration")] [SerializeField]
        private float _duration = 0.12f;

        [Tooltip("补间缓动")] [SerializeField]
        private Ease _ease = Ease.OutQuad;

        [Tooltip("缩放目标：留空即本节点；挂在 Selectable 根上时可指定子物体（如 Handle）")] [SerializeField]
        private Transform _targetToScale;

        private Tween _tween;
        private float _baseScale;

        /// <summary>补间时长（秒）：宿主的其它演出（如邻牌让位）与它对齐时读这里。</summary>
        public float Duration => _duration;

        private Transform ScaleTarget => _targetToScale != null ? _targetToScale : transform;

        // 自驱基准只在这时取一次：自驱对象是静态的；位姿会被外部改写的对象一律用被驱动模式
        private void Awake() => _baseScale = ScaleTarget.localScale.x;

        // 自驱复位：失活即清补间并把缩放写回基准，防池化实例带着放大态被复用
        private void OnDisable()
        {
            Kill();
            if (!_selfDriven) return;
            ScaleTarget.localScale = Vector3.one * _baseScale;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (_selfDriven) PlayScale(_baseScale * _scaleFactor);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_selfDriven) PlayScale(_baseScale);
        }

        #region 通道：宿主驱动与交接

        /// <summary>补到悬停目标缩放（被驱动模式）：宿主裁决出悬停的那一刻调用。</summary>
        public void PlayHoverScale() => PlayScale(_hoverScale);

        /// <summary>补到任意缩放：自驱的进出与宿主给的还原值都走这里。</summary>
        public void PlayScale(float target_) =>
            UiTween.PlayScalar(ref _tween, () => ScaleTarget.localScale.x,
                value_ => ScaleTarget.localScale = Vector3.one * value_, target_, _duration, _ease);

        /// <summary>交接通道：杀补间并清槽，之后由别的写者接管（宿主重排、回收前调）。</summary>
        public void Kill()
        {
            _tween?.Kill();
            _tween = null;
        }

        #endregion
    }
}
