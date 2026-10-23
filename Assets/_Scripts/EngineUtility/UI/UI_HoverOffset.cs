using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>悬停偏移执行器：一条补间、两种触发。自驱模式（默认）指针进入时按父级空间把偏移量加到"进入时的位姿"上、
    /// 离开复位；离开时用进入时缓存的基座矩形兜底判定——指针仍落在原位置内就不复位，位移类演出不会因指针脱卡来回抖。
    /// 被驱动模式关掉 _selfDriven，由宿主算好位姿后调 Play / Kill，本类只当通道写者。
    /// 偏移量以父级空间画布像素计（随父级朝向与缩放，上下左右任意）；只做位移，朝向一律由调用方给。</summary>
    public sealed class UI_HoverOffset : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("自驱模式：开＝自己接指针事件；关＝由宿主调 Play(世界位姿) 驱动")] [SerializeField]
        private bool _selfDriven = true;

        [Tooltip("偏移量（父级空间画布像素）：自驱模式按它移动，x 向右、y 向上；被驱动模式下只作为宿主计算位姿的只读参数")] [SerializeField]
        private Vector2 _offsetPx = new(0f, 120f);

        [Tooltip("补间时长（秒）：宿主的其它演出要与它对齐时读 Duration")] [SerializeField]
        private float _duration = 0.12f;

        private Tween _tween;
        private RectTransform _rect;
        private Camera _canvasCamera;

        // 自驱会话：进入时的基座位姿与世界尺寸（离开时按基座矩形做并集命中判定）
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector2 _baseSize;
        private Vector2 _basePivot;
        private bool _hovered;

        /// <summary>偏移量（父级空间画布像素）：被驱动模式下由宿主读它算偏移后的位姿。</summary>
        public Vector2 OffsetPx => _offsetPx;

        /// <summary>补间时长（秒）：宿主的其它演出与它对齐时读这里。</summary>
        public float Duration => _duration;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvasCamera = canvas ? canvas.worldCamera : null;
        }

        // 自驱复位：失活即清补间并写回进入前的位姿，防池化实例带着偏移态被复用
        private void OnDisable()
        {
            Kill();
            if (!_selfDriven || !_hovered || !_rect) return;
            _hovered = false;
            _rect.SetPositionAndRotation(_basePosition, _baseRotation);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_selfDriven || _hovered) return;
            _hovered = true;
            _basePosition = _rect.position;
            _baseRotation = _rect.rotation;
            _baseSize = Vector2.Scale(_rect.rect.size, _rect.lossyScale);
            _basePivot = _rect.pivot;
            Transform space = _rect.parent ? _rect.parent : _rect;
            Play(_basePosition + space.TransformVector(new Vector3(_offsetPx.x, _offsetPx.y, 0f)), _baseRotation);
        }

        public void OnPointerExit(PointerEventData event_)
        {
            if (!_selfDriven || !_hovered) return;
            // 并集命中：指针仍落在进入时的基座矩形内就不复位（偏移把对象移开了，原位置照样算悬停）
            if (PointerInBaseRect(event_.position)) return;
            _hovered = false;
            Play(_basePosition, _baseRotation);
        }

        #region 通道：宿主驱动与交接

        /// <summary>补到给定世界位姿（被驱动模式）：悬停进入与退出都由宿主算好位姿后调用。</summary>
        public void Play(Vector3 worldPosition_, Quaternion worldRotation_) =>
            _tween = UiTween.PlayPose(ref _tween, _rect, worldPosition_, worldRotation_, _duration);

        /// <summary>交接通道：杀补间并清槽，之后由别的写者接管（宿主重排、回收前调）。</summary>
        public void Kill()
        {
            _tween?.Kill();
            _tween = null;
        }

        #endregion

        // 自驱的并集判定：屏幕点先换算到本矩形所在画布平面，再按进入时的基座矩形判点
        private bool PointerInBaseRect(Vector2 screen_)
        {
            Vector3 world = UiGeometry.ScreenToPlaneWorld(_rect, screen_, _canvasCamera);
            return UiGeometry.Contains(world, _basePosition, _baseRotation, _baseSize, _basePivot);
        }
    }
}
