using System.Collections.Generic;
using TMPro;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>开发日志面板（常驻主菜单画布）：左栏目录条点选，右栏正文显示被选中条目的正文。
    /// 条目由场景手摆（_barContentDict），本面板只负责绑定点击、默认选中首条与关闭。</summary>
    public sealed class UI_DevelopLogPanel : UI_Panel
    {
        [Tooltip("左栏目录条：场景手摆，顺序即显示顺序")] [SerializeField]
        private List<UI_DevelopLogBar> _barContentDict = new();

        [Tooltip("右栏标题")] [SerializeField] private TextMeshProUGUI _titleText;
        [Tooltip("右栏正文")] [SerializeField] private TextMeshProUGUI _contentText;

        [SerializeField] private Button _closeButton;

        private bool _wired;

        protected override void OnShown()
        {
            // 关闭按钮只接一次；目录条在 OnHidden 会解绑，每次打开都先解再挂
            if (!_wired)
            {
                _wired = true;
                if (_closeButton) _closeButton.onClick.AddListener(Hide);
            }
            foreach (UI_DevelopLogBar bar in _barContentDict)
            {
                bar.Clicked -= WriteContent;
                bar.Clicked += WriteContent;
            }
            // 每次打开都重写首条，顺带把正文高度与滚动范围刷到当前文本
            WriteContent(_barContentDict.Count > 0 ? _barContentDict[0] : null);
        }

        protected override void OnHidden()
        {
            // 收起即解绑：面板是常驻失活体，留着订阅会让下次打开重复挂
            foreach (UI_DevelopLogBar bar in _barContentDict)
                bar.Clicked -= WriteContent;
        }

        // 右栏标题与正文一起换：默认选中首条与点选目录走同一条路
        private void WriteContent(UI_DevelopLogBar bar_)
        {
            if (_titleText) _titleText.text = bar_ ? bar_.Title : string.Empty;
            if (_contentText) _contentText.text = bar_ ? bar_.Description : string.Empty;
            // 正文的 ContentSizeFitter 会被父级 LayoutGroup 压回固定高：先按文本撑高，当帧重排 content 再拉回顶部

            if (!_contentText) return;
            ScrollRect scroll = _contentText.GetComponentInParent<ScrollRect>();
            if (!scroll) return;
            _contentText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _contentText.preferredHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1f;
        }
    }
}