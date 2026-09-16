using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Splines;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌视图：手牌与出牌全流程的编排者——按领域事件生成与回收卡面、播放抽牌入场与归宿演出、沿曲线排布手牌、逐帧裁决悬停、把拖拽落点转成打出命令。
    /// 只发命令与消费事件，不判断任何规则：能量够不够、目标合不合法一律由域侧校验，被拒的命令由拒绝事件退回手牌、阶段对账兜底。
    /// 悬停的唯一裁决者：每帧算出指针下的第一张手牌并只驱动它，重叠手牌不会互相争抢；卡牌能力由预制体配置，未配能力的卡不参与裁决。</summary>
    public class Obj_HandView : MonoBehaviour
    {
        private const int DefaultAimSlot = 0;

        [Tooltip("手牌卡面预制体：池化实例化用，实例挂在 HandView 之下")] [SerializeField]
        private Obj_Card _cardPrefab;

        [Tooltip("手牌排布曲线：曲线上某一点的位置与切线方向决定一张手牌的位置与朝向")] [SerializeField]
        private SplineContainer _handSpline;

        [Tooltip("出牌区：松手时以卡牌根物体所在那一个点判定是否落入其中")] [SerializeField]
        private Obj_PlayCardZone _dropZone;

        [Tooltip("抽牌堆视图：抽牌入场的起飞锚点")] [SerializeField]
        private Obj_DrawPileView _drawPileView;

        [Tooltip("弃牌堆视图：弃牌归宿的飞行终点")] [SerializeField]
        private Obj_DiscardPileView _discardPileView;

        [Tooltip("消耗堆视图：消耗与遗忘卡归宿的飞行终点")] [SerializeField]
        private Obj_ExhaustPileView _exhaustPileView;

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

        private readonly List<Obj_Card> _hand = new();
        private readonly Dictionary<CardModel, Obj_Card> _views = new();
        private readonly HashSet<Obj_Card> _pendingPlays = new();
        private readonly HashSet<Obj_Card> _entering = new();
        // 手牌卡 → 悬停能力缓存：抽牌时注册、池回收时注销，悬停裁决与进出全走它，热路径不再逐卡查组件
        private readonly Dictionary<Obj_Card, Comp_CardHover> _hovers = new();

        private ObjectPool<Obj_Card> _cardPool;
        private Obj_Card _draggedCard;
        private Obj_Card _hoveredCard;
        private Camera _camera;
        private BattleCommandDispatcher _commandDispatcher;

        private void Awake()
        {
            _camera = Camera.main;
            _cardPool = new ObjectPool<Obj_Card>(
                () => Instantiate(_cardPrefab, transform),
                actionOnGet: view_ => view_.gameObject.SetActive(true),
                actionOnRelease: view_ =>
                {
                    view_.KillMotion();
                    view_.gameObject.SetActive(false);
                    _hovers.Remove(view_);  // 回收即注销悬停缓存，池复用由抽牌注册覆盖写
                },
                defaultCapacity: 10,
                maxSize: 32);
        }

        #region 接线：命令门面转发打出意图，事件门面订阅本视图负责的全部领域事件

        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_)
        {
            _commandDispatcher = commandDispatcher_;
            eventDispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            eventDispatcher_.Register<BE_CardDiscarded>(OnCardDiscardedAsync);
            eventDispatcher_.Register<BE_CardExhausted>(OnCardExhaustedAsync);
            eventDispatcher_.Register<BE_CardErased>(OnCardErasedAsync);
            eventDispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            eventDispatcher_.Register<BE_TurnPhaseChanged>(OnTurnPhaseChangedAsync);
        }

        #endregion

        #region 出牌受理：校验在手上后转发命令门面，不直接触碰战斗循环

        public void RequestPlay(CardModel card_, int aimSlot_)
        {
            if (!_views.ContainsKey(card_)) return;
            _commandDispatcher.PlayCard(card_, aimSlot_);
        }

        // 抓起：掐掉在飞补间，随后由拖拽能力跟手
        public void OnCardGrabbed(Obj_Card card_)
        {
            _draggedCard = card_;
            card_.KillMotion();
        }

        // 松手：以卡牌根物体所在那一个点判定是否落进出牌区，命中即视为选定目标打出，未命中飞回手牌槽位
        public void OnCardReleased(Obj_Card card_)
        {
            _draggedCard = null;
            if (_dropZone == null || !_dropZone.Accepts(card_.transform.position))
            {
                ReturnToHandAsync(card_).Forget();
                return;
            }

            PlayToZone(card_, _dropZone);
        }

        // 拖拽被强制中断（组件禁用或回收）时直接落回槽位，不播补间
        public void OnCardDragCancelled(Obj_Card card_)
        {
            _draggedCard = null;
            PlaceToHandSlot(card_);
        }

        #endregion

        // 悬停裁决：每帧裁定指针下唯一一张悬停牌，目标不变则不动，变化时先撤旧再进新；
        // 不依赖指针事件，指针不动时手牌增删也会被下次裁定接管或让位，重叠手牌不再互相争抢
        private void Update()
        {
            Obj_Card target = ResolveHoverTarget(out Comp_CardHover hover);
            if (target == _hoveredCard) return;

            if (_hoveredCard) ExitHover(_hoveredCard);
            _hoveredCard = target;
            hover?.EnterHover();
        }

        // 撤下悬停：仍在手牌可交互时按当前槽位目标值回位，离手 / 被拖走 / 位置补间中只清状态交给后续补间
        private void ExitHover(Obj_Card card_)
        {
            if (!_hovers.TryGetValue(card_, out Comp_CardHover hover)) return;
            if (!IsInteractive(card_))
            {
                hover.ClearHover();
                return;
            }

            EvaluateFanSlot(_hand.IndexOf(card_), _hand.Count, out _, out _, out float scale, out int order);
            hover.ExitHover(order, scale);
        }

        // 悬停闸门：给出"此刻该不该响应指针"的判定，供逐帧裁决与悬停退出共用
        // 只有手牌中的牌可悬停：有牌被拖拽时手牌保持安静，已悬置在出牌区等待结算的牌不再响应，
        // 位置补间在飞的牌也不响应——否则悬停会被补间逐帧覆盖层序，还会跟着飞到半途坐标
        public bool IsInteractive(Obj_Card card_) =>
            !_draggedCard && _hand.Contains(card_) && !_pendingPlays.Contains(card_) &&
            !_entering.Contains(card_) && !card_.IsMoving;

        // 指针下的第一张手牌：命中与层序比较都在此完成，重叠时取层序最高者（视觉最上层），并顺手带出它的悬停能力
        private Obj_Card ResolveHoverTarget(out Comp_CardHover hover_)
        {
            Vector3 pointer = PointerWorldPosition();
            Obj_Card best = null;
            hover_ = null;
            foreach (Obj_Card card in _hand)
            {
                if (!IsInteractive(card) || !card.ContainsPoint(pointer)) continue;
                if (!_hovers.TryGetValue(card, out Comp_CardHover hover)) continue; // 只认登记过悬停能力的卡，纯展示卡不参与裁决
                if (best && card.Order <= best.Order) continue;
                best = card;
                hover_ = hover;
            }

            return best;
        }

        // 屏幕指针 → 世界坐标（投射到卡牌所在的 z=0 平面）：悬停命中与拖拽跟手共用
        public Vector3 PointerWorldPosition()
        {
            Vector3 pointer = Input.mousePosition;
            pointer.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(pointer);
        }

        // 抽牌入场：池中取出 → 抽牌堆位置零尺寸 → 放大的同时滑翔到手牌槽位 → 落位后收正尺寸
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            CardModel model = drawn_.Card;
            Obj_Card view = _cardPool.Get();
            view.Bind(model);
            view.SetHost(this);
            _views[model] = view;
            _hand.Add(view);
            _entering.Add(view);

            // 悬停能力只在抽牌时查一次组件，未配悬停能力的卡不登记、不参与悬停裁决
            if (view.TryGetComponent<Comp_CardHover>(out Comp_CardHover hover)) _hovers[view] = hover;

            EvaluateFanSlot(_hand.Count - 1, _hand.Count, out Vector3 position, out Quaternion rotation, out _,
                out int order);
            view.Place(_drawPileView.Anchor, Quaternion.identity, order);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次
            ApplyFanLayout(view);

            await UniTask.WhenAll(
                view.ScaleToAsync(_drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, order, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_views.TryGetValue(model, out Obj_Card bound) || bound != view)
            {
                _entering.Remove(view);
                return;
            }

            await view.ScaleToAsync(1f, _drawSettleDuration);
            _entering.Remove(view);
        }

        // 轮到执行：卡已离手并停在出牌区，等入队补间播完即可，结算期间不飞不缩
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_views.TryGetValue(played_.Card, out Obj_Card view)) return;
            _pendingPlays.Remove(view);
            _hand.Remove(view);
            ApplyFanLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        private UniTask OnCardDiscardedAsync(BE_CardDiscarded discarded_) =>
            ExitToPileAsync(discarded_.Card, _discardPileView.Anchor);

        private UniTask OnCardExhaustedAsync(BE_CardExhausted exhausted_) =>
            ExitToPileAsync(exhausted_.Card, _exhaustPileView.Anchor);

        // 遗忘按既定口径同样飞往消耗堆，此处只播表现形式，永久移除与否由域侧决定
        private UniTask OnCardErasedAsync(BE_CardErased erased_) =>
            ExitToPileAsync(erased_.Card, _exhaustPileView.Anchor);

        // 出牌被拒：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；未在悬置集合里说明已另行处置，不重复动它
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (!_views.TryGetValue(rejected_.Card, out Obj_Card view)) return UniTask.CompletedTask;
            if (!_pendingPlays.Remove(view)) return UniTask.CompletedTask;

            _dropZone.Remove(view);
            return ReturnToHandAsync(view);
        }

        // 阶段对账：非玩家回合等无事件的静默丢弃在此兜底，阶段一变就把仍悬着的出牌请求退回手牌
        private UniTask OnTurnPhaseChangedAsync(BE_TurnPhaseChanged changed_)
        {
            if (_pendingPlays.Count == 0) return UniTask.CompletedTask;

            // 循环体只动出牌区队列与补间，不改本集合，可边遍历边退
            foreach (Obj_Card card in _pendingPlays)
            {
                _dropZone.Remove(card);
                ReturnToHandAsync(card).Forget();
            }

            _pendingPlays.Clear();

            return UniTask.CompletedTask;
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆并缩到 0 → 回收进池
        private async UniTask ExitToPileAsync(CardModel model_, Vector3 destination_)
        {
            if (!_views.TryGetValue(model_, out Obj_Card view)) return;
            _pendingPlays.Remove(view);
            _hand.Remove(view);
            _dropZone.Remove(view);
            ApplyFanLayout();

            await UniTask.WhenAll(
                view.MoveToAsync(destination_, Quaternion.identity, _exitOrder, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            _views.Remove(model_);
            _cardPool.Release(view);
        }

        // 落点即出牌：目标选择尚未实现，槽位固定打首个敌人，等敌人视图落地后再由落点决定
        private void PlayToZone(Obj_Card card_, Obj_PlayCardZone zone_)
        {
            _pendingPlays.Add(card_);
            zone_.Enqueue(card_);
            RequestPlay(card_.Card, DefaultAimSlot);
        }

        // 手牌变化即重排：被拖拽、被悬停、已挂出牌的牌与指定的例外牌（刚起飞的新牌）不参与，例外牌由抽牌补间自己负责落位
        private void ApplyFanLayout(Obj_Card except_ = null)
        {
            int count = _hand.Count;
            for (int i = 0; i < count; i++)
            {
                Obj_Card card = _hand[i];
                if (card == except_ || card == _draggedCard || card == _hoveredCard ||
                    _pendingPlays.Contains(card)) continue;

                EvaluateFanSlot(i, count, out Vector3 position, out Quaternion rotation, out float scale,
                    out int order);
                card.MoveToAsync(position, rotation, order, _fanMoveDuration).Forget();
                card.ScaleToAsync(scale, _fanMoveDuration).Forget();
            }
        }

        // 第 index 张（共 count 张）在弧线上的位置、朝向、大小与层序：以曲线中点为锚向两侧等距张开，间距超出可用区间时整段自动压缩
        private void EvaluateFanSlot(int index_, int count_, out Vector3 position_, out Quaternion rotation_,
            out float scale_, out int order_)
        {
            float step = count_ <= 1 ? 0f : Mathf.Min(_fanStepT, (_fanEndT - _fanStartT) / (count_ - 1));
            float t = (_fanStartT + _fanEndT) * 0.5f + (index_ - (count_ - 1) * 0.5f) * step;
            _handSpline.Evaluate(t, out float3 position, out float3 tangent, out _);
            position_ = position;
            Vector3 direction = tangent;
            rotation_ = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            scale_ = Mathf.Max(_fanMinScale, 1f - _fanScaleStep * Mathf.Max(0, count_ - _fanFullCount));
            order_ = _handBaseOrder + index_;
        }

        // 直接落回当前槽位：拖拽被中断时用，不播补间
        private void PlaceToHandSlot(Obj_Card card_)
        {
            int index = _hand.IndexOf(card_);
            if (index < 0) return;

            EvaluateFanSlot(index, _hand.Count, out Vector3 position, out Quaternion rotation, out float scale,
                out int order);
            card_.Place(position, rotation, order);
            card_.transform.localScale = Vector3.one * scale;
        }

        private UniTask ReturnToHandAsync(Obj_Card card_)
        {
            int index = _hand.IndexOf(card_);
            if (index < 0) return UniTask.CompletedTask;

            EvaluateFanSlot(index, _hand.Count, out Vector3 position, out Quaternion rotation, out float scale,
                out int order);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, order, _fanMoveDuration),
                card_.ScaleToAsync(scale, _fanMoveDuration));
        }
    }
}