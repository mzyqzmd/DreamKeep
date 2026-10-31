using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>
    /// 按钮悬停切换显隐：指针进入时把目标物体设为 <see cref="_activeOnHover"/>，离开时设为其反值。
    /// </summary>
    public sealed class UI_HoverActive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private MonoBehaviour _targetComponent;
        [SerializeField] private bool _activeOnHover = true; // 悬停时激活、离开时失活；关掉则反向

        public void OnPointerEnter(PointerEventData _) => SetHovered(true);
        public void OnPointerExit(PointerEventData _) => SetHovered(false);

        // 悬停中按钮被禁用/隐藏时收不到 Exit，兜底复位到离开态
        private void OnDisable() => SetHovered(false);

        private void SetHovered(bool value_)
        {
            // 判空必须走 Unity 的隐式 bool（`?.` 只在托管引用为空时才跳过，未接线的序列化引用会照旧调原生方法并抛 UnassignedReference）
            if (_target) _target.SetActive(_activeOnHover == value_);
            if (_targetComponent) _targetComponent.enabled = _activeOnHover == value_;
        }
    }
}