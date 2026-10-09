using _DreamKeep.GameBattle;
using _DreamKeep.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌区门面：持有 Inspector 配置，在装配点按"手牌区本体 → 手牌流程"建起两个协作者，把宿主契约与每帧驱动转发给它们。
    /// 本体（HandArea）管牌的去向、资格、层归属、槽位排布与悬停裁决；流程（HandPlayFlow）管出牌、瞄准与领域事件演出；
    /// 出牌区（UI_PlayCardZone）是另一块自洽区域。它只对外暴露瞄准事实：瞄准中的牌、指针世界坐标、目标槽位回写口。</summary>
    public sealed class UI_HandView : MonoBehaviour
    {
        [Tooltip("战斗包装预制体：池化实例化用，实例挂在手牌层之下，里面内嵌牌面")] [SerializeField]
        private UI_CardHolder _cardPrefab;

        [Tooltip("出牌区：自洽的一块区域——松手时以卡牌的锚点（下沿中点）是否落在它自己的矩形内判定，队位以它内部的点为基准")]
        [FormerlySerializedAs("dropZone")]
        [SerializeField]
        private UI_PlayCardZone _dropZone;

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

        [Header("目标箭头")] [Tooltip("瞄准 AnyEnemy 卡时从卡牌中心指向指针的箭头：位姿由手牌流程每帧驱动")] [SerializeField]
        private UI_CardArrow _cardArrow;

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
        private const int NoAimSlot = -1; // 瞄准尚未指向任何敌人：松手时据此取消，不打出
        private const float FanMoveDuration = 0.28f; // 手牌重排（让位、回位）的补间时长
        private const float DrawGrowDuration = 0.25f; // 抽牌飞行时长：从抽牌堆零尺寸滑到手牌槽位
        private const float DrawGrowOvershoot = 1.1f; // 抽牌途中的放大倍率：落位后再收回槽位尺寸
        private const float DrawSettleDuration = 0.15f; // 飞行到位后收回常态尺寸的时长
        private const float ExitDuration = 0.25f; // 离场飞向牌堆并缩到 0 的时长
        private const float ReshuffleFlyDuration = 0.2f; // 单张洗回牌从弃牌堆飞到抽牌堆的时长
        private const float ReshuffleFlyScale = 0.2f; // 洗回飞行牌的起始缩放
        private const float ReshuffleFlySpreadPx = 72f; // 洗回起落点的散开幅度
        private const float ReshuffleFlySpreadMaxPx = 240f; // 洗回牌流的总跨度上限
        private const float FlyJitterPx = 24f; // 弃牌波与洗回波起落点的随机抖动幅度（画布像素）
        private const float UnplayableDropPx = -110f; // 非我方回合整手牌的下沉量：负值向下

        /// <summary>非瞄准卡的默认目标槽位：按存活列表最前一个结算（前敌、全体、随机等落点解析不读它）。
        /// 单位侧的目标提示驱动未在瞄准时也回写它，故此处是唯一来源。</summary>
        public const int DefaultAimSlot = 0;

        private HandArea _area;
        private HandPlayFlow _playFlow;
        private int _aimSlot = DefaultAimSlot;

        // 装配：按依赖方向线性构造，参数即"谁被谁要"，构造期无前向引用、无闭包。
        // 画布相机在这时解析（不靠 Awake，避免与容器装配的先后次序打架）
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_,
            Rng rng_)
        {
            Camera canvasCamera = GetComponentInParent<Canvas>().worldCamera;
            _area = new HandArea(_cardPrefab, this, _dropZone, _fanCenterPoint, _handLayer, _flyLayer, _focusLayer,
                canvasCamera, _fanHalfRunPx, _fanStepPx, _fanSagPx, _fanTiltMaxDeg, FanMoveDuration, _fanFullCount,
                _fanScaleStep, _fanMinScale, UnplayableDropPx);
            _playFlow = new HandPlayFlow(_area, _dropZone, commandDispatcher_, rng_, drawPile, discardPile, exhaustPile,
                _cardArrow, DrawGrowDuration, DrawGrowOvershoot, DrawSettleDuration, ExitDuration,
                ReshuffleFlyDuration, ReshuffleFlyScale, ReshuffleFlySpreadPx,
                ReshuffleFlySpreadMaxPx, FlyJitterPx);
            _playFlow.Register(eventDispatcher_);
        }

        #region 出牌受理与瞄准事实：抓起、松手与中断一律转交出牌流程，本视图不判断规则

        // 抓起：AnyEnemy 卡一按下就把目标槽位清成"未指向"，防极快操作下默认值被当成有效目标打出去
        public void OnCardGrabbed(UI_CardHolder card_)
        {
            _playFlow.Grab(card_);
            if (card_.Card.IsAnyEnemyTarget) SetAimSlot(NoAimSlot);
        }

        public void OnCardReleased(UI_CardHolder card_) => _playFlow.Release(card_, CurrentAimSlot);

        public void OnCardDragCancelled(UI_CardHolder card_) => _playFlow.CancelDrag(card_);

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，转交手牌流程刷新箭头位姿（并带上当前是否已指向合法目标）
        public void OnAimDragged(UI_CardHolder card_) => _playFlow.OnAimDragged(card_, _aimSlot >= 0);

        /// <summary>这张牌此刻是不是走瞄准模式（AnyEnemy 卡按下即成立）：包装据此决定跟手滑行还是把拖动上报给宿主。</summary>
        public bool IsAiming(UI_CardHolder card_) => _playFlow.IsAiming(card_);

        /// <summary>当前瞄准的目标槽位（存活敌人列表下标，-1 = 未指向敌人）：打牌命令与目标提示预览共用这一处来源，由单位侧每帧回写。</summary>
        public int CurrentAimSlot => _aimSlot;

        /// <summary>回写当前瞄准的目标槽位：-1 = 未指向任何敌人，松手时据此取消。</summary>
        public void SetAimSlot(int slot_) => _aimSlot = slot_;

        /// <summary>正在瞄准的牌：拖拽中、本回合可打，且走瞄准模式或锚点（下沿中点）落在出牌区矩形内；没在瞄准时为 null。</summary>
        public UI_CardHolder AimedCard =>
            _area.Dragged is UI_CardHolder dragged && _area.CardsPlayable &&
            (_playFlow.IsAiming(dragged) || _dropZone.Accepts(dragged.WorldPosition))
                ? dragged
                : null;

        #endregion

        // 抓取资格：本回合可打、这张牌真在手上（在手牌列、没在入场飞行、没让位给出牌区等结算）、没有别的牌正被拖拽。
        // 动画一律不参与，滑行合拢中的牌照收；指针事件本来就送到最上层那张牌，所以这里只回答"它能不能抓"
        public bool CanGrab(UI_CardHolder card_) => _area.IsInteractive(card_);

        // 指针在画布平面上的世界坐标：拖拽跟手用，换算收在手牌区本体里
        public Vector3 PointerWorldPosition() => _area.PointerWorldPosition();

        // 每帧驱动：本体裁决悬停进出，流程刷新瞄准中的箭头并兜底校正瞄准状态；目标槽位不小于 0 即已指向合法目标，随帧下发做命中高亮
        private void Update()
        {
            _area.Tick();
            _playFlow.Tick(_aimSlot >= 0);
        }
    }
}
