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
            WireOnce();
            BindBars();
            SelectFirst();
        }

        protected override void OnHidden() => UnbindBars();

        #region 接线、目录绑定与正文切换

        private void WireOnce()
        {
            if (_wired) return;
            _wired = true;
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        private void BindBars()
        {
            UnbindBars();
            foreach (UI_DevelopLogBar bar in _barContentDict)
                if (bar)
                    bar.Clicked += OnBarClicked;
        }

        // 收起即解绑：面板是常驻失活体，留着订阅会让下次打开重复挂
        private void UnbindBars()
        {
            foreach (UI_DevelopLogBar bar in _barContentDict)
                if (bar)
                    bar.Clicked -= OnBarClicked;
        }

        private void SelectFirst() => WriteContent(_barContentDict.Count > 0 ? _barContentDict[0] : null);

        private void OnBarClicked(UI_DevelopLogBar bar_) => WriteContent(bar_);

        // 右栏标题与正文一起换：默认选中首条与点选目录走同一条路
        private void WriteContent(UI_DevelopLogBar bar_)
        {
            if (_titleText) _titleText.text = bar_ ? bar_.Title : string.Empty;
            if (_contentText) _contentText.text = bar_ ? bar_.Description : string.Empty;
        }

        #endregion
    }
}