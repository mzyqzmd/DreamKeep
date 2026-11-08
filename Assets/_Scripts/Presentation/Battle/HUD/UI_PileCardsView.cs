using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>牌堆查看面板：战斗内陈列指定牌堆全部卡的弹层——抽牌/弃牌/消耗三堆共用这一个变体，
    /// 排列规则与底部说明随堆切换：弃牌堆/消耗堆按进入顺序（域序）从左到右、从上到下陈列，
    /// 抽牌堆按类型陈列（诅咒→能力→技能→攻击，同类型内保持真实堆序）。打开期间订阅影响三堆的域事件做池化差分对账——
    /// 不在域集合的实例逐个还池（网格收拢，抽牌时可见卡一张一张消失）、新出现的卡租池追加，
    /// 关闭即停。卡实例纯展示（UI_CardInPile），点击遮罩或关闭按钮收起；
    /// 事件无反注册（与手牌区同口径），处理器自带开合守卫，面板不开零副作用。</summary>
    public sealed class UI_PileCardsView : MonoBehaviour
    {
        [Header("陈列")] [Tooltip("卡实例预制体：牌堆查看卡（四内嵌脸 + 悬停气泡）")] [SerializeField]
        private UI_CardDisplay _cardPrefab;

        [Tooltip("网格挂载点：GridLayoutGroup 的 content，卡实例的出生与陈列父级")] [SerializeField]
        private RectTransform _gridContent;

        [Tooltip("网格所在的滚动视图：打开时把滚动位置拉回顶部")] [SerializeField]
        private ScrollRect _scroll;

        [Tooltip("关闭按钮")] [SerializeField] private Button _closeButton;

        [Header("说明")] [Tooltip("底部说明文本：随当前查看的牌堆切换")] [SerializeField]
        private TextMeshProUGUI _pileTipText;

        [Header("说明文案")] [Tooltip("抽牌堆的说明：面板默认文案就归它用")] [SerializeField] [TextArea(2, 4)]
        private string _drawTip = "回合开始时会在这里抽牌 (顺序打乱)";

        [Tooltip("弃牌堆的说明")] [SerializeField] [TextArea(2, 4)]
        private string _discardTip = "打出或弃置的牌会按进入顺序放在这里，抽牌堆见底时整摞洗回抽牌堆";

        [Tooltip("消耗堆的说明")] [SerializeField] [TextArea(2, 4)]
        private string _exhaustTip = "被消耗的牌会按进入顺序放在这里，本场战斗不会再回到牌堆";

        private BattleQueryDispatcher _queryDispatcher;
        private MonoObjectPool<UI_CardDisplay> _pool;
        private readonly List<UI_CardDisplay> _shown = new(); // 当前陈列的实例（顺序随陈列序对齐）
        private readonly List<CardModel> _domain = new(); // 查询缓冲：当前堆的域内卡序
        private EPileType _kind;
        private bool _open;

        private void OnEnable() => _closeButton.onClick.AddListener(Close);

        // 外部关激活（战斗结束等）时收不到点击关闭，兜底还池收摊
        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
            _open = false;
            ReturnAll();
        }

        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_, BattleEventDispatcher eventDispatcher_,
            IObjectResolver resolver_)
        {
            _queryDispatcher = queryDispatcher_;
            // 池化实例在容器外生成：新建时补一次注入（卡内悬停气泡要 PopFactory），复用实例的依赖早已就位
            _pool = new MonoObjectPool<UI_CardDisplay>(_gridContent, 64,
                onCreate_: instance_ => resolver_.InjectGameObject(instance_.gameObject));
            // 影响三堆的五条事件全订（打出堆与遗忘不碰三堆、不订）：面板开着才对账，关着一条事件零成本
            eventDispatcher_.Register<BE_CardDrawn>(_ => OnPileChanged());
            eventDispatcher_.Register<BE_CardDiscarded>(_ => OnPileChanged());
            eventDispatcher_.Register<BE_CardsDiscarded>(_ => OnPileChanged());
            eventDispatcher_.Register<BE_CardExhausted>(_ => OnPileChanged());
            eventDispatcher_.Register<BE_DiscardReshuffled>(_ => OnPileChanged());
        }

        #region 对外入口：打开指定牌堆

        /// <summary>打开指定牌堆的陈列：写说明文案、激活面板、全量对账一次并回到顶部。</summary>
        public void Open(EPileType type_)
        {
            _open = true;
            _kind = type_;
            _pileTipText.text = type_ switch
            {
                EPileType.Draw => _drawTip,
                EPileType.Discard => _discardTip,
                EPileType.Exhaust => _exhaustTip,
                _ => "如果游戏中看到这句话说明游戏有bug",
            };
            gameObject.SetActive(true);
            Reconcile();
            // 滚动范围按当帧的网格高度算：先强制重排，再把滚动位置拉回顶部
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridContent);
            _scroll.verticalNormalizedPosition = 1f;
        }

        #endregion

        /// <summary>关闭面板：实例全部还池、事件订阅保留（下次打开再对账）。</summary>
        private void Close()
        {
            _open = false;
            ReturnAll();
            gameObject.SetActive(false);
        }

        // 事件入口：五条影响三堆的事件共用（差分同步完成，无演出等待）
        private void OnPileChanged() => Reconcile();

        // 对账三步：归还消失的（逐张收格）、租池新增的（追加实例）、按陈列顺序对齐显示列表与兄弟位；
        // 差分以 CardModel 引用等值（每场战斗的卡实例唯一），洗回抽牌堆这类整摞变动也自然收敛到正确陈列
        private void Reconcile()
        {
            if (!_open) return;
            // 域序填充复用缓冲：弃牌/消耗堆即进入顺序，抽牌堆 index 0 为堆顶
            _queryDispatcher.PileCards(_kind, _domain);

            // 归还：显示中已不在本堆的卡（被抽走/移出/洗走）——逐个还池失活，网格自动收拢
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                if (_domain.Contains(_shown[i].Card)) continue;
                _pool.Release(_shown[i]);
                _shown.RemoveAt(i);
            }

            // 新增：本堆里有而尚未陈列的卡——租池绑定并追加（落位由下面的对齐步骤统一写）
            for (int i = 0; i < _domain.Count; i++)
            {
                if (ShownOf(_domain[i])) continue;
                UI_CardDisplay instance = _pool.Get(_cardPrefab);
                instance.Bind(_domain[i]);
                _shown.Add(instance);
            }

            // 陈列顺序：抽牌堆按类型排（诅咒→能力→技能→攻击，同类型保持真实堆序），弃牌/消耗堆保持域序即进入顺序
            if (_kind == EPileType.Draw) CardSorter.SortByType(_domain);
            // 对齐：按陈列序写兄弟位（序号没变不写，避免白标脏画布）。
            // 显示名单只当"当前陈列中的实例集合"用，没有任何地方读它的顺序，故不必跟着重排
            for (int i = 0; i < _domain.Count; i++)
            {
                UI_CardDisplay instance = ShownOf(_domain[i]);
                if (instance && instance.transform.GetSiblingIndex() != i) instance.transform.SetSiblingIndex(i);
            }
        }

        // 找显示中绑定了指定卡的实例：找不到返回 null
        private UI_CardDisplay ShownOf(CardModel card_)
        {
            for (int i = 0; i < _shown.Count; i++)
                if (_shown[i].Card == card_)
                    return _shown[i];
            return null;
        }

        // 归还全部显示实例：面板收摊用（Close 与外部失活兜底共用）
        private void ReturnAll()
        {
            for (int i = _shown.Count - 1; i >= 0; i--) _pool.Release(_shown[i]);
            _shown.Clear();
        }
    }
}