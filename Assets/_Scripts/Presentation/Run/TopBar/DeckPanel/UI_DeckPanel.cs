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
    /// 再点当前模式翻转方向（默认序不翻转），当前模式的箭头图标染高亮色；点卡进全屏特写，特写里可用升级预览开关
    /// 查看升级后形态（临时可变副本展示、永不写回域）。卡组变更只发生在面板关闭的跑图段，故打开时全量重建、
    /// 不订阅任何事件；战斗段同样可开（顶栏常驻），与战斗内三堆查看（UI_PileCardsView）互不干扰。</summary>
    public sealed class UI_DeckPanel : MonoBehaviour
    {
        [Header("陈列")] [Tooltip("卡实例预制体：网格与特写共用（网格态由本面板缩到格尺寸）")] [SerializeField]
        private UI_CardInDeck _cardPrefab;

        [Tooltip("网格挂载点：GridLayoutGroup 所在节点，卡实例的出生与陈列父级")] [SerializeField]
        private RectTransform _gridContent;

        [Tooltip("网格所在滚动视图：打开时把滚动位置拉回顶部")] [SerializeField] private ScrollRect _scroll;

        [Header("排序")] [Tooltip("排序按钮：下标对齐 EDeckSortMode（费用/类型/默认）")] [SerializeField]
        private Button[] _sortButtons;

        [Tooltip("各排序按钮的箭头图标，与 _sortButtons 同序")] [SerializeField] private Image[] _sortIcons;

        [Tooltip("当前模式的箭头高亮色")] [SerializeField] private Color _activeIconColor = new(1f, 0.69f, 0.4f);

        [Header("特写")] [Tooltip("特写层：点网格卡打开、点背板关闭")] [SerializeField]
        private GameObject _concreteCardView;

        [Tooltip("特写背板的关闭按钮：挂在特写层根 Image 上")] [SerializeField] private Button _concreteCloseButton;

        [Tooltip("特写卡：全尺寸展示当前查看的卡")] [SerializeField] private UI_CardInDeck _closeupCard;

        [Tooltip("升级预览开关：不可升级的卡整体隐藏")] [SerializeField] private Button _upgradeToggle;

        [Tooltip("开关的勾选图标：随预览态换图")] [SerializeField] private Image _toggleIcon;

        [Tooltip("预览开的勾选图")] [SerializeField] private Sprite _tickSprite;

        [Tooltip("预览关的空心框图")] [SerializeField] private Sprite _untickSprite;

        [Header("开合")] [Tooltip("关闭按钮")] [SerializeField] private Button _closeButton;

        private RunState _run;
        private MonoObjectPool<UI_CardInDeck> _pool;
        private readonly List<CardModel> _display = new(); // 当前排序下的陈列序缓冲
        private readonly List<UI_CardInDeck> _shown = new(); // 当前在网格里的实例
        private Color[] _idleIconColors; // 各箭头图标的本色缓存：高亮只写当前模式，其余恢复这里
        private EDeckSortMode _mode = EDeckSortMode.Cost; // 当前排序模式：初始费用低→高
        private bool _descending; // 当前模式内是否翻转方向；默认序无方向恒 false
        private CardModel _shownCard; // 特写正在看的卡（域内真卡的引用）
        private bool _previewing; // 特写是否处于升级预览态

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
            _concreteCloseButton.onClick.AddListener(CloseConcrete);
            _upgradeToggle.onClick.AddListener(TogglePreview);

            // 箭头图标本色缓存：高亮只写当前模式的图标，其余时刻恢复这里的初始色
            _idleIconColors = new Color[_sortIcons.Length];
            for (int i = 0; i < _sortIcons.Length; i++)
                if (_sortIcons[i])
                    _idleIconColors[i] = _sortIcons[i].color;
        }

        // 外部失活兜底：特写收起、网格还池，实例不滞留池外
        private void OnDisable()
        {
            CloseConcrete();
            ReturnAll();
        }

        #region 对外入口：开合面板

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 与地图/奖励面板同住一个 Canvas，取出时压到最上层
            RefreshGrid();
            // 滚动范围按当帧的网格高度算：先强制重排，再把滚动位置拉回顶部
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridContent);
            _scroll.verticalNormalizedPosition = 1f;
        }

        public void Hide() => gameObject.SetActive(false); // 清理（特写收起、网格还池）由 OnDisable 兜底，避免双跑

        public void Toggle()
        {
            if (gameObject.activeSelf) Hide();
            else Show();
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
                instance.Bind(_display[i], OpenConcrete);
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

        #region 特写与升级预览

        // 点网格卡：记下真卡、预览复位，按可升级性摆出预览开关后开特写层
        private void OpenConcrete(CardModel card_)
        {
            _shownCard = card_;
            _previewing = false;
            // 先开层再写内容：特写卡与开关的 Awake 随首次激活补跑，Bind 依赖它们
            _concreteCardView.SetActive(true);
            _upgradeToggle.gameObject.SetActive(card_.CanUpgrade);
            RefreshConcrete();
        }

        // 特写刷新：预览开 = 临时可变副本套升级补丁后展示（不写回域），预览关 = 原卡；勾选图随态换
        private void RefreshConcrete()
        {
            if (_previewing)
            {
                CardModel preview = _shownCard.ToMutable();
                preview.Upgrade();
                _closeupCard.Bind(preview, null);
                _toggleIcon.sprite = _tickSprite;
            }
            else
            {
                _closeupCard.Bind(_shownCard, null);
                _toggleIcon.sprite = _untickSprite;
            }
        }

        // 预览开关入口：翻转预览态并重刷特写（开关只在可升级卡上可见）
        private void TogglePreview()
        {
            _previewing = !_previewing;
            RefreshConcrete();
        }

        // 关特写：收层并复位预览态，下次打开另一张卡从头来
        private void CloseConcrete()
        {
            if (!_concreteCardView.activeSelf) return;
            _concreteCardView.SetActive(false);
        }

        #endregion

        // 归还全部网格实例：Hide 与外部失活兜底共用
        private void ReturnAll()
        {
            for (int i = _shown.Count - 1; i >= 0; i--) _pool.Release(_shown[i]);
            _shown.Clear();
        }
    }
}
