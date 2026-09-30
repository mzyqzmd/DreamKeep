using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    public class Tool_ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
    [SerializeField] private float _scaleOnHover = 1.2f;
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _ease = Ease.OutBack;
    [SerializeField] private Transform _targetToScale; // 留空=缩放自身；挂在 Selectable 根上时指定子物体（如 Handle）

    private Vector3 _baseScale;
    private bool _hovered;
    private bool _selected;
    private Tween _tween;

    private Transform ScaleTarget => _targetToScale != null ? _targetToScale : transform;

    private void Awake() => _baseScale = ScaleTarget.localScale;

    private void PlayScale(bool enlarge_)
    {
        _tween?.Kill();
        _tween = ScaleTarget.DOScale(enlarge_ ? _baseScale * _scaleOnHover : _baseScale, _duration)
            .SetEase(_ease);
    }

    public void OnPointerEnter(PointerEventData _) => SetHovered(true);
    public void OnPointerExit(PointerEventData _) => SetHovered(false);
    public void OnSelect(BaseEventData _) => SetSelected(true);
    public void OnDeselect(BaseEventData _) => SetSelected(false);

    private void SetHovered(bool value_)
    {
        _hovered = value_;
        PlayScale(_hovered || _selected);
    }

    private void SetSelected(bool value_)
    {
        _selected = value_;
        PlayScale(_hovered || _selected);
    }

    private void OnDestroy() => _tween?.Kill();
    }
}