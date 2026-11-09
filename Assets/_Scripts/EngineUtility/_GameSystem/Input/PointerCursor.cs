using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>指针光标：按下时换成点击态图案，全部松开回到普通态。左右中任意键都算按下——
    /// 地图上右键画墨迹、中键擦除，按下的视觉反馈没有理由不同。
    /// 只在按下态真正翻转的那一帧调一次 <c>Cursor.SetCursor</c>，不逐帧调用。
    /// 挂常驻 GameRoot（随 UI_SingletonObject 进 DontDestroyOnLoad），全局生效、场景切换不丢。</summary>
    public sealed class PointerCursor : MonoBehaviour
    {
        [Tooltip("普通态光标：箭头尖端对准指针")] [SerializeField]
        private Texture2D _idleCursor;

        [Tooltip("点击态光标：按住期间显示，尖端同样对准指针")] [SerializeField]
        private Texture2D _pressedCursor;

        [Tooltip("鼠标热点：原点在图片左下角，填箭头尖端的像素坐标")] [SerializeField]
        private Vector2 _hotspot = new(12f, 3f);

        private bool _pressed; // 当前显示的是不是点击态

        private void OnEnable() => Apply(false);

        // 退出运行或组件关闭时还原系统光标：直接杀进程之外的关闭路径都不会留下自定义图案
        private void OnDisable()
        {
            _pressed = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void Update()
        {
            bool pressed = PointerInput.AnyPressed;
            if (pressed == _pressed) return;
            Apply(pressed);
        }

        // 换图只走这里：同态重复请求直接忽略，缺图时保持系统光标不动
        private void Apply(bool pressed_)
        {
            _pressed = pressed_;
            Texture2D texture = pressed_ ? _pressedCursor : _idleCursor;
            if (!texture) return;
            Cursor.SetCursor(texture, _hotspot, CursorMode.Auto);
        }
    }
}