using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>缩放执行器：一条补间、两种触发。自驱模式（默认，挂到任意对象上即生效）自己接指针事件，
    /// 进入按"进入时的基准 × 倍数"放大、离开回到基准，失活即复位；被驱动模式关掉 _selfDriven，
    /// 由宿主在裁决出悬停的那一刻调 PlayScaleRelative(HoverScale) 补到基准 × 悬停倍数，或 PlayScale 补到任意给定值。
    /// 可选呼吸因子由兄弟 UI_HoverBreath 提供（订阅其相位事件重写缩放），没挂就恒 1，本类仍是唯一写者。
    /// 等比缩放（写入 Vector3.one × 值）；同通道独占由 UiTween 保证（建前先杀旧值）；
    /// 通道交接与回收由宿主调 Kill，本类不猜宿主状态。</summary>
    public sealed class UI_HoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("自驱模式：开＝自己接指针事件（挂到按钮等对象上即生效）；关＝由宿主调 PlayScaleRelative / PlayScale 驱动")] [SerializeField]
        private bool _selfDriven = true;

        [Tooltip("悬停放大倍数：悬停补到基准 × 该值、非悬停回基准，自驱与被驱共用")] [SerializeField]
        private float _hoverScale = 1.05f;

        [Tooltip("补间时长（秒）：宿主的其它演出要与它对齐时读 Duration")] [SerializeField]
        private float _duration = 0.12f;

        [Tooltip("补间缓动")] [SerializeField] private Ease _ease = Ease.OutQuad;

        [Tooltip("缩放目标：留空即本节点；挂在 Selectable 根上时可指定子物体（如 Handle）")] [SerializeField]
        private Transform _targetToScale;

        private Tween _tween;
        private float _baseScale;
        private float _current; // 悬停/复位补间的当前值
        private bool _scaleCached;
        private UI_HoverBreath _breath;

        /// <summary>悬停放大倍数（基准 × 它 = 悬停目标）：宿主借读它走 PlayScaleRelative。</summary>
        public float HoverScale => _hoverScale;

        private Transform ScaleTarget => _targetToScale != null ? _targetToScale : transform;

        // 自驱基准只取一次：自驱对象是静态的；位姿会被外部改写的对象一律用被驱动模式。
        // 呼吸/悬停可能在 Awake 之前（面板失活期）就被调用，所以也走这里懒取，避免拿 0 当基准
        private void Awake() => CacheScale();

        private void CacheScale()
        {
            if (_scaleCached) return;
            _scaleCached = true;
            _baseScale = ScaleTarget.localScale.x;
            _current = _baseScale;
            // 呼吸因子来自兄弟组件（可选）：同 GameObject 同生共死，订阅后无需退订
            if (!_breath) _breath = GetComponent<UI_HoverBreath>();
            if (_breath) _breath.PhaseChanged += ApplyScale;
        }

        // 自驱复位：失活即清补间并把缩放写回基准，防池化实例带着放大态被复用
        private void OnDisable()
        {
            Kill();
            if (!_selfDriven) return;
            CacheScale();
            _current = _baseScale;
            ApplyScale();
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (_selfDriven) PlayScaleRelative(_hoverScale);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_selfDriven) PlayScale(_baseScale);
        }

        #region 通道：宿主驱动与交接

        /// <summary>补到基准缩放 × 倍数（相对缩放）：宿主裁决出悬停时传 HoverScale 补到悬停目标，图例高亮与按倍数还原也走它。</summary>
        public void PlayScaleRelative(float factor_) => PlayScale(_baseScale * factor_);

        /// <summary>补到任意缩放：自驱的进出与宿主给的还原值都走这里。</summary>
        public void PlayScale(float target_)
        {
            CacheScale();
            UiTween.PlayScalar(ref _tween, () => _current, value_ =>
            {
                _current = value_;
                ApplyScale();
            }, target_, _duration, _ease);
        }

        /// <summary>同步自驱基准：外部整体缩放（如整行适配）写完缩放后调用，进入按新基准放大、离开回到新基准。</summary>
        public void SetBaseScale(float baseScale_)
        {
            CacheScale();
            _baseScale = baseScale_;
            _current = baseScale_;
            if (_tween == null) ApplyScale();
        }

        /// <summary>交接通道：杀补间并清槽，之后由别的写者接管（宿主重排、回收前调）。</summary>
        public void Kill()
        {
            _tween?.Kill();
            _tween = null;
        }

        #endregion

        // 唯一写缩放的地方：补间值 × 呼吸因子（无呼吸组件时因子恒 1）
        private void ApplyScale() => ScaleTarget.localScale = Vector3.one * (_current * BreathFactor);

        private float BreathFactor => _breath != null ? _breath.Factor : 1f;
    }
}