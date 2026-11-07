using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>按钮悬停切换显隐：指针进入时把目标设为 <see cref="_activeOnHover"/>，离开时设为其反值。
    /// 另有强制点亮（SetForced，与悬停取或）与强制熄灭（SetSuppressed，压掉悬停）两条宿主通道。</summary>
    public sealed class UI_HoverActive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private MonoBehaviour _targetComponent;
        [SerializeField] private bool _activeOnHover = true; // 悬停时激活、离开时失活；关掉则反向

        private bool _hovered;
        private bool _forced;
        private bool _suppressed;

        /// <summary>强制点亮：模式激活期间目标恒亮（与悬停取或），宿主在模式开/关时调用。</summary>
        public void SetForced(bool forced_)
        {
            _forced = forced_;
            Refresh();
        }

        /// <summary>强制熄灭：另一工具激活时压掉本条目的悬停发光，保证全局只亮一个 GlowIcon。</summary>
        public void SetSuppressed(bool suppressed_)
        {
            _suppressed = suppressed_;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData _) => SetHovered(true);
        public void OnPointerExit(PointerEventData _) => SetHovered(false);

        // 悬停中按钮被禁用/隐藏时收不到 Exit，兜底复位到离开态；强制态属宿主语义，不在此清
        private void OnDisable()
        {
            _hovered = false;
            Refresh();
        }

        private void SetHovered(bool value_)
        {
            _hovered = value_;
            Refresh();
        }

        private void Refresh()
        {
            // 判空必须走 Unity 的隐式 bool（`?.` 只在托管引用为空时才跳过，未接线的序列化引用会照旧调原生方法并抛 UnassignedReference）
            bool active = !_suppressed && _activeOnHover == (_hovered || _forced);
            if (_target) _target.SetActive(active);
            if (_targetComponent) _targetComponent.enabled = active;
        }
    }
}
