using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>悬停偏移执行器：一条补间、两种触发。自驱模式（默认）指针进入时把偏移量加到"进入时的父级本地位姿"上、
    /// 离开复位；离开时用进入时缓存的基座矩形兜底判定——指针仍落在原位置内就不复位，位移类演出不会因指针脱卡来回抖。
    /// 自驱的位移与恢复一律写父级本地位姿：宿主（如滑动的面板）在悬停期间移动时，恢复不会把物件拖回旧的世界位置（防逐次漂移）。
    /// 被驱动模式关掉 _selfDriven，由宿主算好世界位姿后调 Play / Kill，本类只当通道写者。
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

        // 自驱会话：进入时的基座父级本地位姿（恢复用——写父级本地，宿主在悬停期间滑动也不漂）与世界基座矩形（并集命中判定用）
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseWorldPosition;
        private Quaternion _baseWorldRotation;
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
            _canvasCamera = UiGeometry.CanvasCamera(this);
        }

        // 自驱复位：失活即清补间并写回进入前的父级本地位姿，防池化实例带着偏移态被复用
        private void OnDisable()
        {
            Kill();
            if (!_selfDriven || !_hovered || !_rect) return;
            _hovered = false;
            _rect.SetLocalPositionAndRotation(_baseLocalPosition, _baseLocalRotation);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_selfDriven || _hovered) return;
            _hovered = true;
            _baseLocalPosition = _rect.localPosition;
            _baseLocalRotation = _rect.localRotation;
            _baseWorldPosition = _rect.position;
            _baseWorldRotation = _rect.rotation;
            _baseSize = Vector2.Scale(_rect.rect.size, _rect.lossyScale);
            _basePivot = _rect.pivot;
            PlayLocal(_baseLocalPosition + (Vector3)_offsetPx, _baseLocalRotation);
        }

        public void OnPointerExit(PointerEventData event_)
        {
            if (!_selfDriven || !_hovered) return;
            // 并集命中：指针仍落在进入时的基座矩形内就不复位（偏移把对象移开了，原位置照样算悬停）
            if (PointerInBaseRect(event_.position)) return;
            _hovered = false;
            PlayLocal(_baseLocalPosition, _baseLocalRotation);
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

        // 自驱的本地位姿补间：进入的偏移与离开的复位都走这里，目标钉在父级本地、宿主移动不漂
        private void PlayLocal(Vector3 localPosition_, Quaternion localRotation_)
        {
            _tween?.Kill();
            Vector3 fromPosition = _rect.localPosition;
            Quaternion fromRotation = _rect.localRotation;
            float progress = 0f;
            _tween = DOTween.To(() => progress, value_ => progress = value_, 1f, _duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => _rect.SetLocalPositionAndRotation(
                    Vector3.Lerp(fromPosition, localPosition_, progress),
                    Quaternion.Slerp(fromRotation, localRotation_, progress)));
        }

        // 自驱的并集判定：屏幕点先换算到本矩形所在画布平面，再按进入时的基座矩形判点
        private bool PointerInBaseRect(Vector2 screen_)
        {
            Vector3 world = UiGeometry.ScreenToPlaneWorld(_rect, screen_, _canvasCamera);
            return UiGeometry.Contains(world, _baseWorldPosition, _baseWorldRotation, _baseSize, _basePivot);
        }
    }
}
