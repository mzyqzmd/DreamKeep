using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌视图（门面）：手牌与出牌全流程的编排者——按领域事件生成与回收包装、播放抽牌入场与归宿演出、沿浅弧排布手牌、逐帧裁决悬停、把拖拽落点转成打出命令。
    /// 只发命令与消费事件，不判断任何规则：能量够不够、目标合不合法一律由域侧校验，被拒的命令由拒绝事件退回手牌、阶段对账兜底。
    /// 本体只做三件事：持有 Inspector 配置、在装配点按"手牌本体与交互资格 → 手牌空间 → 打牌与表现"线性建起三个协作者、把宿主契约与每帧驱动转发给它们；
    /// 其余职责分在 HandCardRegistry（卡池、手牌索引、层归属与交互状态判据）、HandSpace（弧线槽位求解、指针与画布坐标换算、悬停裁决）、HandPlayFlow（拖拽出牌与全部事件演出）里。
    /// 结构是两层架子：手牌中心节点挂在它名下，队列层、手牌层、飞行层、交互层四个卡层再挂在中心节点名下——
    /// 中心节点即手牌弧的顶点（正中那张牌的锚点），也是全部卡牌坐标的框架（在 Scene 里拖它即整手挪位，缩放与旋转同样生效），层的兄弟顺序则决定卡牌之间的先后；
    /// 战斗包装（UI_CardHolder）实例挂在各自该在的层里，包装里内嵌一张纯展示牌面。
    /// 配置分两档：反复看效果的手感值（中心节点与七个排布参数）留在 Inspector，入场、离场、洗回的时长与不可打下沉量收进本类的私有常量，改这些要重新编译。</summary>
    public class UI_HandView : MonoBehaviour
    {
        [Tooltip("战斗包装预制体：池化实例化用，实例挂在手牌层之下，里面内嵌牌面")] [SerializeField]
        private UI_CardHolder _cardPrefab;

        [Tooltip("出牌区：松手时以卡牌锚点是否落在它自己的矩形内判定——那个矩形同时是落点区与打出牌的展示区")] [SerializeField]
        private UI_PlayCardZone dropZone;

        [Tooltip("抽牌堆视图：抽牌入场的起飞锚点")] [SerializeField]
        private UI_DrawPile drawPile;

        [Tooltip("弃牌堆视图：弃牌归宿的飞行终点")] [SerializeField]
        private UI_DiscardPile discardPile;

        [Tooltip("消耗堆视图：消耗与遗忘卡归宿的飞行终点")] [SerializeField]
        private UI_ExhaustPile exhaustPile;

        [Header("卡牌层")] [Tooltip("队列层：出牌区里等待结算的牌（排在最前，画在所有卡层之下）")] [SerializeField]
        private Transform _queueLayer;

        [Tooltip("手牌层：手牌停在这里，层内按手牌序号排位（靠右压靠左）")] [SerializeField]
        private Transform _handLayer;

        [Tooltip("飞行层：离场、洗回与遗忘借道飞行的牌")] [SerializeField]
        private Transform _flyLayer;

        [Tooltip("交互层：悬停与拖拽的牌（排在最后，永远画在最上层）")] [SerializeField]
        private Transform _focusLayer;

        // 约定：本节点的 pivot 必须是 (0.5,0)——四个卡层随它全拉伸，层矩形的下沿中点就是卡牌锚点的参照点，也是本节点的局部原点，故节点铺多大都不影响坐标系
        [Header("手牌中心")] [Tooltip("手牌中心节点：手牌弧的顶点，也就是正中那张牌的锚点。四个卡层挂在它名下，排布坐标全部相对它算——在 Scene 里拖它即可整手挪位，缩放与旋转同样生效")] [SerializeField]
        private RectTransform _fanCenter;

        [Header("手牌排布")] [Tooltip("手牌可用半宽（画布像素）：张数多到单张间距撑破它时整段压缩，横向跨度不会超出该值")] [SerializeField]
        private float _fanHalfRunPx = 570f;

        [Tooltip("相邻手牌的水平间距（画布像素）：调大更松散，撑破可用半宽时自动压缩")] [SerializeField]
        private float _fanStepPx = 155.5f;

        [Tooltip("手牌浅弧的拱高（画布像素）：两端比正中那张低出的量，0 即一条直线")] [SerializeField]
        private float _fanSagPx = 14.4f;

        [Tooltip("手牌两端的最大倾角（度）：越靠两端向外倾得越多，正中回正")] [SerializeField]
        private float _fanTiltMaxDeg = 0.96f;

        [Tooltip("不缩小的手牌张数上限：不超过该张数时手牌都是常态尺寸 1")] [SerializeField]
        private int _fanFullCount = 5;

        [Tooltip("超出上限后每多一张手牌缩小的量：0.06 即每张缩小 6%")] [SerializeField]
        private float _fanScaleStep = 0.08f;

        [Tooltip("手牌缩放下限：手牌再多也不小于该值")] [SerializeField]
        private float _fanMinScale = 0.6f;

        // 演出与手感的固定值：曾经是 Inspector 上的可调字段，现收进代码常量（改这些要重新编译，也不再随场景序列化）
        private const float FanMoveDuration = 0.28f;         // 手牌重排（让位、回位）的补间时长
        private const float DrawGrowDuration = 0.25f;        // 抽牌飞行时长：从抽牌堆零尺寸滑到手牌槽位
        private const float DrawGrowOvershoot = 1.2f;        // 抽牌途中的放大倍率：落位后再收回槽位尺寸
        private const float DrawSettleDuration = 0.15f;      // 飞行到位后收回常态尺寸的时长
        private const float ExitDuration = 0.25f;            // 离场飞向牌堆并缩到 0 的时长
        private const float ReshuffleFlyDuration = 0.2f;     // 单张洗回牌从弃牌堆飞到抽牌堆的时长
        private const float ReshuffleFlyScale = 0.4f;        // 洗回飞行牌的起始缩放
        private const float ReshuffleFlyStagger = 0.05f;     // 相邻两张洗回牌的起飞间隔
        private const float ReshuffleFlySpreadPx = 72f;      // 洗回起落点的散开幅度
        private const float ReshuffleFlySpreadMaxPx = 240f;  // 洗回牌流的总跨度上限
        private const float ReshuffleMaxDuration = 0.6f;     // 洗回起飞段的总时长上限
        private const float UnplayableDropPx = -115.2f;      // 非我方回合整手牌的下沉量：负值向下

        private HandCardRegistry _registry;
        private HandSpace _space;
        private HandPlayFlow _playFlow;

        // 装配：按依赖方向线性构造，参数即"谁被谁要"，构造期无前向引用、无闭包。
        // 画布相机与卡牌坐标框架都在装配点解析（不靠 Awake，避免与容器装配的先后次序打架）：
        // 框架就是手牌中心节点，四个卡层与包装实例都挂在它名下，出牌区的判定与队位也统一按它换算
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_)
        {
            Camera canvasCamera = GetComponentInParent<Canvas>().worldCamera;
            _registry = new HandCardRegistry(_cardPrefab, this, dropZone, _handLayer, _queueLayer, _flyLayer, _focusLayer);
            dropZone.SetCardFrame(_fanCenter);
            _space = new HandSpace(_fanCenter, canvasCamera, _fanHalfRunPx, _fanStepPx, _fanSagPx, _fanTiltMaxDeg,
                FanMoveDuration, _fanFullCount, _fanScaleStep, _fanMinScale, UnplayableDropPx, _registry);
            _playFlow = new HandPlayFlow(_registry, _space, dropZone, commandDispatcher_, drawPile,
                discardPile, exhaustPile, DrawGrowDuration, DrawGrowOvershoot, DrawSettleDuration,
                ExitDuration, ReshuffleFlyDuration, ReshuffleFlyScale, ReshuffleFlyStagger,
                ReshuffleFlySpreadPx, ReshuffleFlySpreadMaxPx, ReshuffleMaxDuration);
            _playFlow.Register(eventDispatcher_);
        }

        #region 出牌受理：抓起、松手与中断一律转交出牌流程，本视图不判断规则

        public void OnCardGrabbed(UI_CardHolder card_) => _playFlow.Grab(card_);

        public void OnCardReleased(UI_CardHolder card_) => _playFlow.Release(card_);

        public void OnCardDragCancelled(UI_CardHolder card_) => _playFlow.CancelDrag(card_);

        #endregion

        // 抓取资格：本回合可打、没被拖拽或挂出、没在入场飞行、位置补间已停，且手牌已全部抽完落稳。
        // 指针事件本来就送到层级命中出的最上层那张牌，所以这里只回答"它能不能抓"
        public bool CanGrab(UI_CardHolder card_) => _registry.IsGrabbable(card_);

        // 指针在卡牌坐标框架（手牌中心节点）里的本地坐标：拖拽跟手用，换算收在手牌空间里
        public Vector2 PointerLocalPosition() => _space.PointerLocalPosition();

        // 每帧只驱动悬停裁决一次：要不要换悬停目标、怎么进怎么退都由手牌空间决定
        private void Update() => _space.Tick();
    }
}
