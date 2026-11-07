using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>地图工具栏：画笔/橡皮按钮切换对应模式、清空按钮一次清掉全部墨迹（无快捷键）；
    /// 订阅墨迹的工具态事件，按 ActiveTool 单一真源让 GlowIcon 全局恒亮一个，失活兜底复位强制态。</summary>
    public class UI_MapToolBar : MonoBehaviour
    {
        [Tooltip("画笔按钮：点击切换画笔模式")] [SerializeField]
        private Button _quillButton;

        [Tooltip("橡皮按钮：点击切换橡皮模式")] [SerializeField]
        private Button _eraserButton;

        [Tooltip("清空按钮：点击一次清空全部笔迹（无快捷键）")] [SerializeField]
        private Button _clearButton;

        [Tooltip("画笔条目的 GlowIcon 悬停组件（模式激活时强制点亮）")] [SerializeField]
        private UI_HoverActive _quillGlow;

        [Tooltip("橡皮条目的 GlowIcon 悬停组件（模式激活时强制点亮）")] [SerializeField]
        private UI_HoverActive _eraserGlow;

        [Tooltip("清空条目的悬停组件（有工具激活时压掉其悬停光，保证全局只亮一个）")] [SerializeField]
        private UI_HoverActive _clearGlow;

        [Tooltip("墨迹控制器（工具状态与清空的真源）")] [SerializeField]
        private UI_MapInk _ink;

        // 面板 inactive 不跑 Awake：接线推迟到首次激活；墨迹的模式订阅靠注入早于一切交互，无时序问题
        private void Awake()
        {
            if (_quillButton) _quillButton.onClick.AddListener(OnQuillClicked);
            if (_eraserButton) _eraserButton.onClick.AddListener(OnEraserClicked);
            if (_clearButton) _clearButton.onClick.AddListener(OnClearClicked);
            if (_ink) _ink.ModeChanged += RefreshGlow;
        }

        private void OnDestroy()
        {
            if (_ink != null) _ink.ModeChanged -= RefreshGlow;
        }

        // 面板收起兜底：清掉强制态与抑制态，避免重开地图时残留
        private void OnDisable()
        {
            if (_quillGlow) { _quillGlow.SetSuppressed(false); _quillGlow.SetForced(false); }
            if (_eraserGlow) { _eraserGlow.SetSuppressed(false); _eraserGlow.SetForced(false); }
            if (_clearGlow) _clearGlow.SetSuppressed(false);
        }

        private void OnQuillClicked() => _ink.TogglePenMode();

        private void OnEraserClicked() => _ink.ToggleEraserMode();

        private void OnClearClicked() => _ink.ClearAllStrokes();

        // 单一真源：手势优先的 ActiveTool 决定谁亮，另一个连悬停光都被压掉，全局恒亮一个
        private void RefreshGlow()
        {
            EMapInkTool tool = _ink.ActiveTool;
            if (_quillGlow)
            {
                _quillGlow.SetSuppressed(tool == EMapInkTool.Eraser);
                _quillGlow.SetForced(tool == EMapInkTool.Pen);
            }

            if (_eraserGlow)
            {
                _eraserGlow.SetSuppressed(tool == EMapInkTool.Pen);
                _eraserGlow.SetForced(tool == EMapInkTool.Eraser);
            }

            if (_clearGlow) _clearGlow.SetSuppressed(tool != EMapInkTool.None);
        }
    }
}
