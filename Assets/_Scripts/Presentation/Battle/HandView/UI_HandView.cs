using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌区门面：手牌区与出牌区对外的装配者与宿主。按领域事件生成与回收包装、播放抽牌入场与归宿演出、
    /// 沿浅弧排布手牌、逐帧裁决悬停、把拖拽落点转成打出命令这些事都不在这里，本体只做三件事：
    /// 持有 Inspector 配置、在装配点按"手牌区本体 → 打牌与表现"线性建起两个协作者、把宿主契约与每帧驱动转发给它们。
    /// 职责分在 HandArea（手牌区本体：台账、资格、层归属、以手牌区的点为准的槽位与排布、指针换算与悬停裁决）与
    /// HandPlayFlow（抓起/松手/取消、回合对账、九个领域事件的演出与向域侧发命令）里。
    /// 出牌区（UI_PlayCardZone）是自洽的另一区：队列状态、队列点与落点判定都在它内部，手牌区只单向问它
    /// "在队里吗 / 命中吗 / 收下 / 还回 / 等停稳"，它不认识手牌区，也不判断任何规则。
    /// 结构是两层架子：卡层只负责挂载与遮挡（层内兄弟顺序决定卡牌之间的先后），卡牌位置一律由"区域点 + 画布像素偏移"
    /// 换算成世界姿态后写入，故在 Scene 里拖手牌区的点即整手挪位，缩放与旋转同样生效。
    /// 配置分两档：反复看效果的手感值（手牌区的点与七个排布参数）留在 Inspector，入场、离场、洗回的时长与不可打下沉量
    /// 收进本类的私有常量，改这些要重新编译。</summary>
    public class UI_HandView : MonoBehaviour
    {
        [Tooltip("战斗包装预制体：池化实例化用，实例挂在手牌层之下，里面内嵌牌面")] [SerializeField]
        private UI_CardHolder _cardPrefab;

        [Tooltip("出牌区：自洽的一块区域——松手时以卡牌的锚点（下沿中点）是否落在它自己的矩形内判定，队位以它内部的点为基准")] [SerializeField]
        private UI_PlayCardZone dropZone;

        [Tooltip("抽牌堆视图：抽牌入场的起飞锚点")] [SerializeField]
        private UI_DrawPile drawPile;

        [Tooltip("弃牌堆视图：弃牌归宿的飞行终点")] [SerializeField]
        private UI_DiscardPile discardPile;

        [Tooltip("消耗堆视图：消耗与遗忘卡归宿的飞行终点")] [SerializeField]
        private UI_ExhaustPile exhaustPile;

        [Header("卡牌层")] [Tooltip("飞行层：离场、洗回与遗忘借道飞行的牌")] [SerializeField]
        private RectTransform _flyLayer;

        [Tooltip("手牌层：手牌停在这里，层内按手牌序号排位（靠右压靠左）")] [SerializeField]
        private RectTransform _handLayer;


        [Tooltip("交互层：悬停与拖拽的牌（排在最后，永远画在最上层）")] [SerializeField]
        private RectTransform _focusLayer;

        [Header("手牌区")]
        [Tooltip("手牌区的点：手牌弧的顶点，也就是正中那张牌的锚点。全部槽位都按它换算成世界姿态——在 Scene 里拖它即整手挪位，缩放与旋转同样生效")]
        [SerializeField]
        private RectTransform _fanCenterPoint;

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
        private const float FanMoveDuration = 0.28f; // 手牌重排（让位、回位）的补间时长
        private const float DrawGrowDuration = 0.25f; // 抽牌飞行时长：从抽牌堆零尺寸滑到手牌槽位
        private const float DrawGrowOvershoot = 1.1f; // 抽牌途中的放大倍率：落位后再收回槽位尺寸
        private const float DrawSettleDuration = 0.15f; // 飞行到位后收回常态尺寸的时长
        private const float ExitDuration = 0.25f; // 离场飞向牌堆并缩到 0 的时长
        private const float ReshuffleFlyDuration = 0.2f; // 单张洗回牌从弃牌堆飞到抽牌堆的时长
        private const float ReshuffleFlyScale = 0.2f; // 洗回飞行牌的起始缩放
        private const float ReshuffleFlyStagger = 0.05f; // 相邻两张洗回牌的起飞间隔
        private const float ReshuffleFlySpreadPx = 72f; // 洗回起落点的散开幅度
        private const float ReshuffleFlySpreadMaxPx = 240f; // 洗回牌流的总跨度上限
        private const float ReshuffleMaxDuration = 0.8f; // 洗回起飞段的总时长上限
        private const float UnplayableDropPx = -110f; // 非我方回合整手牌的下沉量：负值向下

        private HandArea _cards;
        private HandPlayFlow _playFlow;

        // 装配：按依赖方向线性构造，参数即"谁被谁要"，构造期无前向引用、无闭包。
        // 画布相机在装配点解析（不靠 Awake，避免与容器装配的先后次序打架），手牌区的点与三个卡层随参数下发给本体
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_)
        {
            Camera canvasCamera = GetComponentInParent<Canvas>().worldCamera;
            _cards = new HandArea(_cardPrefab, this, dropZone, _fanCenterPoint, _handLayer, _flyLayer, _focusLayer,
                canvasCamera, _fanHalfRunPx, _fanStepPx, _fanSagPx, _fanTiltMaxDeg, FanMoveDuration, _fanFullCount,
                _fanScaleStep, _fanMinScale, UnplayableDropPx);
            _playFlow = new HandPlayFlow(_cards, dropZone, commandDispatcher_, drawPile, discardPile, exhaustPile,
                DrawGrowDuration, DrawGrowOvershoot, DrawSettleDuration, ExitDuration, ReshuffleFlyDuration,
                ReshuffleFlyScale, ReshuffleFlyStagger, ReshuffleFlySpreadPx, ReshuffleFlySpreadMaxPx,
                ReshuffleMaxDuration);
            _playFlow.Register(eventDispatcher_);
        }

        #region 出牌受理：抓起、松手与中断一律转交出牌流程，本视图不判断规则

        public void OnCardGrabbed(UI_CardHolder card_) => _playFlow.Grab(card_);

        public void OnCardReleased(UI_CardHolder card_) => _playFlow.Release(card_);

        public void OnCardDragCancelled(UI_CardHolder card_) => _playFlow.CancelDrag(card_);

        #endregion

        // 抓取资格：本回合可打、这张牌真在手上（在手牌列、没在入场飞行、没让位给出牌区等结算）、没有别的牌正被拖拽。
        // 动画一律不参与，滑行合拢中的牌照收；指针事件本来就送到层级命中出的最上层那张牌，所以这里只回答"它能不能抓"
        public bool CanGrab(UI_CardHolder card_) => _cards.IsInteractive(card_);

        // 指针在画布平面上的世界坐标：拖拽跟手用，换算收在手牌区本体里
        public Vector3 PointerWorldPosition() => _cards.PointerWorldPosition();

        // 每帧只驱动悬停裁决一次：要不要换悬停目标、怎么进怎么退都由手牌区本体决定
        private void Update() => _cards.Tick();
    }
}