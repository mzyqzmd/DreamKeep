using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>整局卡组查看面板：网格陈列 RunState.Player.Deck 全部卡，费用/类型/默认序三种排序——点按钮切模式、
    /// 再点当前模式翻转方向（默认序不翻转），当前模式的箭头图标染高亮色；左/右键点卡打开统一卡牌详情特写
    /// （UI_ConcreteCardView，实例化在本面板名下，展示口径归特写自己的开关行、永不写回域）。卡组变更只发生在
    /// 面板关闭期间（跑图段的删牌/锻造、战斗内遗忘的结算当刻就会写进局内卡组），且面板打开时无法出牌，
    /// 故打开时全量重建、不订阅任何事件；战斗段同样可开（顶栏常驻），与战斗内三堆查看（UI_PileCardsView）互不干扰。
    /// 开合能力继承自 UI_Panel，刷新逻辑落在 OnShown。</summary>
    public sealed class UI_DeckPanel : UI_Panel
    {
        [Header("陈列")] [Tooltip("卡实例预制体：网格与特写共用（网格态由本面板缩到格尺寸）")] [SerializeField]
        private UI_CardInDeck _cardPrefab;

        [Tooltip("网格挂载点：GridLayoutGroup 所在节点，卡实例的出生与陈列父级")] [SerializeField]
        private RectTransform _gridContent;

        [Tooltip("网格所在滚动视图：打开时把滚动位置拉回顶部")] [SerializeField]
        private ScrollRect _scroll;

        [Header("排序")] [Tooltip("排序按钮：下标对齐 EDeckSortMode（费用/类型/默认）")] [SerializeField]
        private Button[] _sortButtons;

        [Tooltip("各排序按钮的箭头图标，与 _sortButtons 同序")] [SerializeField]
        private Image[] _sortIcons;

        [Tooltip("当前模式的箭头高亮色")] [SerializeField]
        private Color _activeIconColor = new(1f, 0.69f, 0.4f);


        [Header("特写")] [Tooltip("统一卡牌详情特写预制体：左/右键点网格卡打开，实例化在本面板名下")] [SerializeField]
        private UI_ConcreteCardView _concrete; // 懒实例化的特写层：面板关时一并收起

        [Header("开合")] [Tooltip("关闭按钮")] [SerializeField]
        private Button _closeButton;

        private RunState _run;
        private MonoObjectPool<UI_CardInDeck> _pool;
        private readonly List<CardModel> _display = new(); // 当前排序下的陈列序缓冲
        private readonly List<UI_CardInDeck> _shown = new(); // 当前在网格里的实例
        private Color[] _idleIconColors; // 各箭头图标的本色缓存：高亮只写当前模式，其余恢复这里
        private EDeckSortMode _mode = EDeckSortMode.Default; // 当前排序模式：获得牌的顺序
        private bool _descending; // 当前模式内是否翻转方向；默认序无方向恒 false

        [Inject]
        public void Construct(RunState run_, IObjectResolver resolver_)
        {
            _run = run_;
            // 池化实例在容器外生成：新建时补一次注入（卡内悬停气泡要 PopFactory），复用实例的依赖早已就位
            _pool = new MonoObjectPool<UI_CardInDeck>(_gridContent, 64,
                onCreate_: instance_ => resolver_.InjectGameObject(instance_.gameObject));

            for (int i = 0; i < _sortButtons.Length; i++)
            {
                int index = i; // 闭包捕获按下标分派
                _sortButtons[i].onClick.AddListener(() => OnSortButtonClicked(index));
            }

            _closeButton.onClick.AddListener(Hide);

            // 箭头图标本色缓存：高亮只写当前模式的图标，其余时刻恢复这里的初始色
            _idleIconColors = new Color[_sortIcons.Length];
            for (int i = 0; i < _sortIcons.Length; i++)
                if (_sortIcons[i])
                    _idleIconColors[i] = _sortIcons[i].color;
        }

        // 外部失活兜底：特写收起、网格还池，实例不滞留池外
        private void OnDisable()
        {
            if (_concrete) _concrete.Close();
            ReturnAll();
        }

        #region 对外入口：开合面板

        protected override void OnShown()
        {
            RefreshGrid();
            // 滚动范围按当帧的网格高度算：先强制重排，再把滚动位置拉回顶部
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridContent);
            _scroll.verticalNormalizedPosition = 1f;
        }

        #endregion

        #region 网格重建：排序→还池→重租→高亮

        // 全量重建：按当前模式与方向排出陈列序，还掉旧实例再逐张租新（n 为整副卡组，重建成本可忽略）
        private void RefreshGrid()
        {
            BuildDisplayOrder();
            ReturnAll();
            for (int i = 0; i < _display.Count; i++)
            {
                UI_CardInDeck instance = _pool.Get(_cardPrefab);
                // 网格按兄弟序摆格子而池复用不改兄弟位：取出即按陈列序钉位，否则排序只换逻辑序不换显示
                instance.transform.SetSiblingIndex(i);
                instance.Bind(_display[i], OpenConcrete, OpenConcrete);
                _shown.Add(instance);
            }

            RefreshSortHighlight();
        }

        // 陈列序 = 局内卡组的引用列表；费用/类型模式经 CardSorter 原位排序（稳定、不写回域），默认序即加入顺序
        private void BuildDisplayOrder()
        {
            _display.Clear();
            IReadOnlyList<CardModel> deck = _run.Player.Deck.Cards;
            for (int i = 0; i < deck.Count; i++) _display.Add(deck[i]);

            if (_mode == EDeckSortMode.Cost) CardSorter.SortByCost(_display, _descending);
            else if (_mode == EDeckSortMode.Type) CardSorter.SortByType(_display, _descending);
        }

        // 高亮当前模式：箭头图标写高亮色，其余写回各自的本色
        private void RefreshSortHighlight()
        {
            for (int i = 0; i < _sortIcons.Length; i++)
            {
                if (!_sortIcons[i]) continue;
                _sortIcons[i].color = i == (int)_mode ? _activeIconColor : _idleIconColors[i];
            }
        }

        // 排序按钮入口：点别的按钮切模式并回到该模式默认方向；再点当前模式翻转方向（默认序无方向，不翻转）
        private void OnSortButtonClicked(int index_)
        {
            if (index_ == (int)_mode && _mode != EDeckSortMode.Default) _descending = !_descending;
            else
            {
                _mode = (EDeckSortMode)index_;
                _descending = false;
            }

            RefreshGrid();
        }

        #endregion

        #region 特写：左/右键开统一详情特写

        // 点网格卡：把真卡交给统一特写（展示口径归它的开关行）
        private void OpenConcrete(CardModel card_) => _concrete.Open(card_);

        #endregion

        // 归还全部网格实例：Hide 与外部失活兜底共用
        private void ReturnAll()
        {
            for (int i = _shown.Count - 1; i >= 0; i--) _pool.Release(_shown[i]);
            _shown.Clear();
        }
    }
}