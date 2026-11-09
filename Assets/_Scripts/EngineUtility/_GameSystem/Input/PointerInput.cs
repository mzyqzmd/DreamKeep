using UnityEngine;
using UnityEngine.InputSystem;

namespace _DreamKeep.EngineUtility
{
    /// <summary>指针原始输入的唯一入口：位置 + 左/右/中三键的按下、松开、持续按住。
    /// 只负责读取，不含任何业务判定——"右键恒为快捷键、画笔模式下左键接管""同帧启动待选的那次左键不算"
    /// 这类规则一律留在消费方。内部全部判空：无指针设备、设备热插拔时调用方不必再写 Pointer.current != null。</summary>
    public static class PointerInput
    {
        #region 位置

        /// <summary>指针屏幕坐标：无指针设备时为 false 并给默认值，调用方据此跳过本次处理。</summary>
        public static bool TryGetScreenPosition(out Vector2 position_)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                position_ = default;
                return false;
            }

            position_ = pointer.position.ReadValue();
            return true;
        }

        #endregion

        #region 按键三态：左 / 右 / 中各自按下、松开、持续按住

        public static bool LeftPressedThisFrame => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool LeftReleasedThisFrame => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        public static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;

        public static bool RightPressedThisFrame => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        public static bool RightReleasedThisFrame => Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;

        public static bool RightHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public static bool MiddlePressedThisFrame => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;

        public static bool MiddleReleasedThisFrame => Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;

        public static bool MiddleHeld => Mouse.current != null && Mouse.current.middleButton.isPressed;

        /// <summary>任一指针按键当前按住（左右中；触屏按 Pointer.press）：光标点击态的唯一判据。
        /// Pointer.pressedButtons 在 Input System 里是 internal，只能按鼠标三键逐个判。</summary>
        public static bool AnyPressed => Mouse.current != null
            ? Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed
            : Pointer.current != null && Pointer.current.press.isPressed;

        #endregion
    }
}
