using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>百科大全根面板：两级开合的唯一编排者——打开先亮三卡菜单页，点入口卡进对应分类子页；
    /// 子页自己的返回键由子页上报返回意图，根面板只负责收起子页、亮出菜单页与关闭整个百科，不代管子页的 UI 引用。</summary>
    public sealed class UI_EncyclopediaPanel : UI_Panel
    {
        [Tooltip("三张入口卡按钮：下标对齐 Panels() 顺序")] [SerializeField]
        private Button[] _subMenuButtons;

        [Tooltip("卡牌总览子页")] [SerializeField] private UI_CardEncyclopediaPanel _cardPanel;
        [Tooltip("遗物收集子页")] [SerializeField] private UI_RelicEncyclopediaPanel _relicPanel;
        [Tooltip("药水研究所子页")] [SerializeField] private UI_PotionEncyclopediaPanel _potionPanel;

        [Tooltip("根返回键：收起整个百科")] [SerializeField]
        private Button _closeButton;

        private void Awake()
        {
            for (int i = 0; i < _subMenuButtons.Length; i++)
            {
                int index = i; // 闭包捕获按下标分派
                _subMenuButtons[i].onClick.AddListener(() => OpenSubPanel(index));
            }

            _cardPanel.CloseRequested += ShowMenu;
            _relicPanel.CloseRequested += ShowMenu;
            _potionPanel.CloseRequested += ShowMenu;
            _closeButton.onClick.AddListener(Hide);
        }

        protected override void OnShown() => ShowMenu();

        protected override void OnHidden() => CloseSubPanels();

        private void OpenSubPanel(int index_)
        {
            CloseSubPanels();
            Panels()[index_].Show();
        }

        private void ShowMenu() => CloseSubPanels();

        // 收起全部子页：返回菜单、切页与整个百科关闭共用；未接线时按引用判空跳过
        private void CloseSubPanels()
        {
            foreach (UI_Panel panel in Panels())
            {
                if (panel)
                    panel.Hide();
            }
        }

        private UI_Panel[] Panels() => new UI_Panel[] { _cardPanel, _relicPanel, _potionPanel };
    }
}