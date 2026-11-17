using System;
using System.Collections.Generic;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌总览：合并所挂卡池去重，烘焙成只读展示模型后网格池化陈列；
    /// 类型 / 稀有度 / 费用三种排序，默认类型正序，再点当前条翻转方向、点别的条复位；
    /// 当前条字体与图标染浅橙，降序时图标 Y/Z 同翻 180°；左 / 右键开统一卡牌详情特写；
    /// 「查看升级」开关把可升级卡换成一次性升级副本并原地重绑、不写域，勾选图随态切换；
    /// 每次打开复位排序与开关态；本页返回键自持，点击只上报返回意图，开合编排归根面板。</summary>
    public sealed class UI_CardEncyclopediaPanel : UI_Panel
    {
        [Header("数据")] [SerializeField] private SO_CardPool[] _cardPools;
        [Header("陈列")] [SerializeField] private UI_CardInDeck _cardPrefab;
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private ScrollRect _scroll;
        [Header("排序")] [SerializeField] private Button[] _sortButtons;
        [SerializeField] private TextMeshProUGUI[] _sortTexts;
        [SerializeField] private Image[] _sortIcons;
        [Tooltip("当前排序条字体与图标的高亮色")] [SerializeField]
        private Color _activeColor = new(1f, 0.671f, 0.208f);
        [Header("特写")] [SerializeField] private UI_ConcreteCardView _concrete;
        [Header("返回")] [Tooltip("本页返回键：点击只上报返回意图")] [SerializeField]
        private Button _closeButton;
        [Header("升级开关")] [SerializeField] private Button _upgradeButton;
        [SerializeField] private Image _toggleIcon;
        [SerializeField] private Sprite _tickSprite;
        [SerializeField] private Sprite _untickSprite;

        public event Action CloseRequested;

        private readonly Dictionary<SO_Card, CardModel> _models = new();
        private readonly List<CardModel> _display = new();
        private readonly List<UI_CardInDeck> _shown = new();
        private MonoObjectPool<UI_CardInDeck> _pool;
        private EEncyclopediaCardSortMode _mode = EEncyclopediaCardSortMode.Type;
        private bool _descending;
        private bool _showUpgraded;
        private PopFactory _popFactory;

        [Inject]
        public void Construct(PopFactory popFactory_)
        {
            _popFactory = popFactory_;
            _concrete.Construct(popFactory_);
        }

        private void Awake()
        {
            _pool = new MonoObjectPool<UI_CardInDeck>(_gridContent, 64,
                onCreate_: item_ => item_.GetComponent<UI_HoverTip>().Construct(_popFactory));

            for (int i = 0; i < _sortButtons.Length; i++)
            {
                int index = i; // 闭包捕获按下标分派
                _sortButtons[i].onClick.AddListener(() => OnSortButtonClicked(index));
            }

            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        private void OnDisable()
        {
            if (_concrete) _concrete.Close();
            ReturnAll();
        }

        protected override void OnShown()
        {
            _mode = EEncyclopediaCardSortMode.Type;
            _descending = false;
            _showUpgraded = false;
            WriteToggleIcon();
            RefreshGrid();
        }

        protected override void OnHidden() => ReturnAll();

        #region 网格陈列：合并池 → 排序 → 重租 → 升级态原地换脸 → 排序视觉

        private void RefreshGrid()
        {
            BuildDisplayOrder();
            ReturnAll();
            for (int i = 0; i < _display.Count; i++)
            {
                UI_CardInDeck item = _pool.Get(_cardPrefab);
                item.transform.SetSiblingIndex(i); // GridLayoutGroup 按兄弟序摆格，池复用不改兄弟位
                item.Bind(DisplayCardOf(_display[i]), OpenConcrete, OpenConcrete);
                _shown.Add(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridContent);
            _scroll.verticalNormalizedPosition = 1f;
            RefreshSortVisual();
        }

        private void OnUpgradeButtonClicked()
        {
            _showUpgraded = !_showUpgraded;
            WriteToggleIcon();
            RebindShown();
        }

        private void WriteToggleIcon() => _toggleIcon.sprite = _showUpgraded ? _tickSprite : _untickSprite;

        // _shown 与 _display 下标一一对应：只换绑展示模型，不动布局、滚动与排序
        private void RebindShown()
        {
            for (int i = 0; i < _shown.Count; i++)
                _shown[i].Bind(DisplayCardOf(_display[i]), OpenConcrete, OpenConcrete);
        }

        // 升级态用一次性可变副本，原型只读、域内真卡零写入；不可升级的卡保持基础形态
        private CardModel DisplayCardOf(CardModel card_)
        {
            if (!_showUpgraded || !card_.CanUpgrade) return card_;
            CardModel copy = card_.ToMutable();
            copy.Upgrade();
            return copy;
        }

        private void BuildDisplayOrder()
        {
            _display.Clear();
            HashSet<SO_Card> seen = new();
            foreach (SO_CardPool pool in _cardPools)
            {
                if (!pool) continue;
                IReadOnlyList<SO_Card> cards = pool.Cards;
                for (int i = 0; i < cards.Count; i++)
                {
                    SO_Card config = cards[i];
                    if (!config || !seen.Add(config)) continue;
                    if (!_models.TryGetValue(config, out CardModel card))
                    {
                        card = CardModelBaker.Bake(config);
                        _models.Add(config, card);
                    }

                    _display.Add(card);
                }
            }

            switch (_mode)
            {
                case EEncyclopediaCardSortMode.Type: CardSorter.SortByType(_display, _descending); break;
                case EEncyclopediaCardSortMode.Rarity: CardSorter.SortByRarity(_display, _descending); break;
                case EEncyclopediaCardSortMode.Cost: CardSorter.SortByCost(_display, _descending); break;
            }
        }

        private void RefreshSortVisual()
        {
            for (int i = 0; i < _sortTexts.Length; i++)
            {
                bool current = i == (int)_mode;
                _sortTexts[i].color = current ? _activeColor : Color.white;
                _sortIcons[i].color = current ? _activeColor : Color.white;
                _sortIcons[i].rectTransform.localEulerAngles = current && _descending
                    ? new Vector3(0f, 180f, 180f)
                    : Vector3.zero;
            }
        }

        private void OnSortButtonClicked(int index_)
        {
            if (index_ == (int)_mode) _descending = !_descending;
            else
            {
                _mode = (EEncyclopediaCardSortMode)index_;
                _descending = false;
            }

            RefreshGrid();
        }

        private void OpenConcrete(CardModel card_) => _concrete.Open(card_);

        // HideOverlays 可能在 Awake 之前调 Hide，故容忍池未建
        private void ReturnAll()
        {
            if (_pool == null) return;
            for (int i = _shown.Count - 1; i >= 0; i--) _pool.Release(_shown[i]);
            _shown.Clear();
        }

        #endregion
    }
}
