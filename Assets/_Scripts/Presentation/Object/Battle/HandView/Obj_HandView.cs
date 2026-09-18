using _DreamKeep.GameBattle;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌视图（门面）：手牌与出牌全流程的编排者——按领域事件生成与回收卡面、播放抽牌入场与归宿演出、沿曲线排布手牌、逐帧裁决悬停、把拖拽落点转成打出命令。
    /// 只发命令与消费事件，不判断任何规则：能量够不够、目标合不合法一律由域侧校验，被拒的命令由拒绝事件退回手牌、阶段对账兜底。
    /// 本体只做三件事：持有 Inspector 配置、在装配点按"登记处 → 闸门 → 排布 → 出牌流程 → 悬停裁决 → 演出播放器"线性建起六个协作者、把宿主契约与每帧驱动转发给它们；
    /// 其余职责分在 HandCardRegistry（卡池与手牌索引）、HandInteractionGate（交互状态与判据）、HandFanLayout（曲线槽位求解与排布）、
    /// HandPlayFlow（拖拽与出牌）、HandHoverArbiter（悬停裁决与指针换算）、HandEventPlayer（事件订阅与全部演出）里。</summary>
    public class Obj_HandView : MonoBehaviour
    {
        [Tooltip("手牌卡面预制体：池化实例化用，实例挂在 HandView 之下")] [SerializeField]
        private Obj_Card _cardPrefab;

        [Tooltip("手牌排布曲线：曲线上某一点的位置与切线方向决定一张手牌的位置与朝向")] [SerializeField]
        private SplineContainer _handSpline;

        [Tooltip("出牌区：松手时以卡牌根物体所在那一个点判定是否落入其中")] [SerializeField]
        private Obj_PlayCardZone _dropZone;

        [Tooltip("抽牌堆视图：抽牌入场的起飞锚点")] [SerializeField]
        private Obj_DrawPile drawPile;

        [Tooltip("弃牌堆视图：弃牌归宿的飞行终点")] [SerializeField]
        private Obj_DiscardPile discardPile;

        [Tooltip("消耗堆视图：消耗与遗忘卡归宿的飞行终点")] [SerializeField]
        private Obj_ExhaustPile exhaustPile;

        [Header("抽牌入场")] [Tooltip("抽牌飞行的时长（秒）：从抽牌堆零尺寸一路滑到手牌槽位")] [SerializeField]
        private float _drawGrowDuration = 0.25f;

        [Tooltip("抽牌途中的放大峰值：略大于常态 1，落位后再收回，调大弹跳感更强")] [SerializeField]
        private float _drawGrowOvershoot = 1.08f;

        [Tooltip("飞行到位后收回常态尺寸的时长（秒），调大收得更柔")] [SerializeField]
        private float _drawSettleDuration = 0.12f;

        [Header("手牌排布")] [Tooltip("手牌占用曲线的起点 t（0~1）：越小左侧展开越靠外")] [SerializeField]
        private float _fanStartT = 0.06f;

        [Tooltip("手牌占用曲线的终点 t（0~1）：越大右侧展开越靠外，需大于起点")] [SerializeField]
        private float _fanEndT = 0.94f;

        [Tooltip("相邻手牌的曲线间距 t：调大更松散；所需区间超出起点~终点时整段自动压缩")] [SerializeField]
        private float _fanStepT = 0.12f;

        [Tooltip("手牌重排的补间时长（秒）：让位、回位都用它")] [SerializeField]
        private float _fanMoveDuration = 0.28f;

        [Tooltip("不缩小的手牌张数上限：不超过该张数时手牌都是常态尺寸 1")] [SerializeField]
        private int _fanFullCount = 5;

        [Tooltip("超出上限后每多一张手牌缩小的量：0.06 即每张缩小 6%")] [SerializeField]
        private float _fanScaleStep = 0.06f;

        [Tooltip("手牌缩放下限：手牌再多也不小于该值")] [SerializeField]
        private float _fanMinScale = 0.7f;

        [Tooltip("手牌层序基准：第 i 张为基准+i，需低于出牌区（60）与悬停（90）层序")] [SerializeField]
        private int _handBaseOrder = 10;

        [Header("归宿离场")] [Tooltip("离场飞向牌堆的时长（秒）：期间卡牌同步缩到 0")] [SerializeField]
        private float _exitDuration = 0.3f;

        [Tooltip("离场飞行期间的层序：需盖过手牌与出牌区，让飞行的牌在最上层可见")] [SerializeField]
        private int _exitOrder = 80;

        [Header("不可打状态")] [Tooltip("非我方回合时整手牌下沉的世界距离：负值向下，按手感调")] [SerializeField]
        private float _unplayableDropY = -0.8f;

        private HandCardRegistry _registry;
        private HandInteractionGate _gate;
        private HandPlayFlow _playFlow;
        private HandHoverArbiter _arbiter;
        private HandEventPlayer _eventPlayer;

        // 装配：按依赖方向线性构造，参数即"谁被谁要"，构造期无前向引用、无闭包；排布只被出牌流程与演出播放器要，故留在局部
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_)
        {
            _registry = new HandCardRegistry(_cardPrefab, transform, this);
            _gate = new HandInteractionGate(_registry);
            HandFanLayout layout = new(_handSpline, _fanStartT, _fanEndT, _fanStepT, _fanMoveDuration,
                _fanFullCount, _fanScaleStep, _fanMinScale, _handBaseOrder, _unplayableDropY, _registry, _gate);
            _playFlow = new HandPlayFlow(_registry, _gate, layout, _dropZone, commandDispatcher_);
            _arbiter = new HandHoverArbiter(_registry, layout, _gate, Camera.main);
            _eventPlayer = new HandEventPlayer(_registry, layout, _gate, _playFlow, _arbiter, _dropZone,
                drawPile, discardPile, exhaustPile, _drawGrowDuration, _drawGrowOvershoot, _drawSettleDuration,
                _exitDuration, _exitOrder);
            _eventPlayer.Register(eventDispatcher_);
        }

        #region 出牌受理：抓起、松手与中断一律转交出牌流程，本视图不判断规则

        public void OnCardGrabbed(Obj_Card card_) => _playFlow.Grab(card_);

        public void OnCardReleased(Obj_Card card_) => _playFlow.Release(card_);

        public void OnCardDragCancelled(Obj_Card card_) => _playFlow.CancelDrag(card_);

        #endregion

        // 抓取裁决：指针下最上层那张能抓的牌（本回合可打、没被拖拽或挂出、没在入场飞行、位置补间已停，且手牌已全部抽完落稳）。
        // 拖拽能力不信任指针事件落在谁身上——Unity 的 2D 物理射线读不到卡牌根 SortingGroup 的层序，重叠交界处的事件会落到下层牌上
        public bool TryResolveGrab(out Obj_Card card_)
        {
            card_ = _registry.ResolveTopCard(PointerWorldPosition(),
                candidate_ => _gate.IsGrabbable(candidate_) && _registry.TryGetDrag(candidate_, out _));
            return card_ != null;
        }

        // 屏幕指针 → 世界坐标（投射到卡牌所在的 z=0 平面）：悬停命中与拖拽跟手共用，换算收在悬停裁决者手上
        public Vector3 PointerWorldPosition() => _arbiter.PointerWorldPosition();

        // 每帧只驱动悬停裁决一次：要不要换悬停目标、怎么进怎么退都由裁决者决定
        private void Update() => _arbiter.Tick();
    }
}
