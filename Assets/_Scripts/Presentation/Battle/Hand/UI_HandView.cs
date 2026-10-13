using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌视图：一块自洽的手牌区——卡池与牌的去向、以手牌区的点为准的排布、悬停裁决与避让、拖拽与瞄准、出牌受理，
    /// 以及十一条领域事件的全部演出，都在装配处一起建起（解析画布相机、建卡池、订阅事件）。
    /// 对外只暴露宿主契约：卡牌问抓取资格与指针坐标并上报指针事件，目标提示驱动读瞄准事实并回写目标槽位。</summary>
    public sealed class UI_HandView : MonoBehaviour
    {
        [Tooltip("战斗包装预制体：池化实例化用，实例挂在手牌层之下，里面内嵌牌面")] [SerializeField]
        private UI_CardBattle _cardPrefab;

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

        [Header("目标箭头")] [Tooltip("瞄准 AnyEnemy 卡时从卡牌中心指向指针的箭头：位姿由本视图每帧驱动")] [SerializeField]
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

        [Tooltip("悬停避让量（画布像素）：悬停某张牌时，它左侧的行内牌左移、右侧的右移各该距离，抬起的牌便不再挡住右邻；0 即不避让")] [SerializeField]
        private float _hoverDodgePx = 160f;

        // 演出与手感的固定值：曾经是 Inspector 上的可调字段，现收进代码常量（改这些要重新编译，也不再随场景序列化）
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

        /// <summary>非瞄准卡的默认目标槽位：按存活列表最前一个结算（前敌、全体、随机等落点解析不读它）。</summary>
        public const int DefaultAimSlot = 0;

        private const int NoAimSlot = -1; // 尚未指向任何敌人：松手时据此取消，不打出

        // 域侧协作者、卡池与手牌台账
        private BattleCommandDispatcher _commandDispatcher;
        private Rng _rng;
        private Camera _canvasCamera;
        private MonoObjectPool<UI_CardBattle> _cardPool;
        private readonly List<UI_CardBattle> _hand = new();
        private readonly Dictionary<CardModel, UI_CardBattle> _views = new();
        private readonly HashSet<UI_CardBattle> _entering = new();

        // 交互状态位：本回合可打、正被拖拽、正被悬停；瞄准状态（哪张牌、箭头是否已显示）只在这里，
        // "按下走哪种交互"由包装读自己的卡回答，不经此路
        private bool _cardsPlayable;
        private UI_CardBattle _draggedCard;
        private UI_CardBattle _hoveredCard;
        private UI_CardBattle _aimingCard;
        private bool _aimingActive;

        // 词条说明气泡列的会话：弹层宿主、当前挂着气泡列的牌与它这一份内容
        // （换卡才重建，并把整列重挂到新卡名下，其后随卡自动跟随）
        private UI_PopTextRoot _popTextRoot;
        private UI_CardBattle _tipCard;
        private readonly List<(string Name, string Description)> _affixTips = new();

        // 当前指向的目标槽位（存活敌人列表下标）：由单位侧的目标提示驱动每帧回写，打牌命令与箭头命中色共用这一处；
        // 瞄准卡一按下先清成"未指向"，防极快操作下默认值被当成有效目标打出去
        private int _aimSlot = DefaultAimSlot;

        // 逐帧射线裁决的复用件：命中列表与指针数据只建一次，避免每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        // 每帧驱动：先裁决悬停进出，再刷新瞄准中的箭头并兜底校正瞄准状态，最后按悬停牌对账词条说明气泡列
        private void Update()
        {
            TickHover();
            TickAim();
            TickAffixTips();
        }

        // 装配：解析画布相机、建卡池、订阅十一条领域事件。画布相机在这时解析（不靠 Awake，避免与容器装配的先后次序打架）
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_,
            Rng rng_, UI_PopTextRoot popTextRoot_)
        {
            _commandDispatcher = commandDispatcher_;
            _rng = rng_;
            _popTextRoot = popTextRoot_;
            _canvasCamera = GetComponentInParent<Canvas>().worldCamera;
            // 出生父节点是手牌层；池上限要容得下"手牌 + 一整摞飞行牌"；失活由池内化，复位回调只写卡自己与宿主这边要收的东西
            _cardPool = new MonoObjectPool<UI_CardBattle>(_handLayer, 64, onReturn_: view_ =>
            {
                view_.KillMotion();
                _entering.Remove(view_); // 回收即注销入场标记与交互身份，防实例复用后残留旧状态
                if (_hoveredCard == view_) _hoveredCard = null;
                if (_draggedCard == view_) _draggedCard = null;
            });

            // 事件清单一处看完，各事件的演出在下面
            eventDispatcher_.Register<BE_DiscardReshuffled>(OnDiscardReshuffledAsync);
            eventDispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            // 单张弃牌：打出结算后的卡离场（消耗/遗忘时域侧自动跳过），将来"弃一张"的卡效也走这里
            eventDispatcher_.Register<BE_CardDiscarded>(discarded_ => ExitToPileAsync(discarded_.Card, discardPile));
            eventDispatcher_.Register<BE_CardsDiscarded>(OnCardsDiscardedAsync);
            eventDispatcher_.Register<BE_CardExhausted>(exhausted_ => ExitToPileAsync(exhausted_.Card, exhaustPile));
            // 遗忘只借道飞往出牌区、不落堆，故不发进堆通知；永久移除与否由域侧决定，这里只播形式
            eventDispatcher_.Register<BE_CardErased>(erased_ => ExitToPileAsync(erased_.Card, null));
            eventDispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            eventDispatcher_.Register<BE_PlayerTurnEnded>(OnPlayerTurnEndedAsync);
            eventDispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #region 出牌受理与瞄准事实

        // 抓起：登记拖拽身份，整张牌换到交互层最上（压过悬停那张）并掐掉在飞补间，随后由包装跟手；
        // 瞄准卡在此登记进瞄准模式——松手分流与箭头生命周期以它为准；同时把目标槽位清成"未指向"，
        // 防极快操作下默认值被当成有效目标打出去
        public void OnCardGrabbed(UI_CardBattle card_)
        {
            _draggedCard = card_;
            MoveToFocus(card_);
            card_.KillMotion();
            _aimingCard = card_.Card.IsAnyEnemyTarget ? card_ : null;
            _aimingActive = false;
            if (_aimingCard != null) _aimSlot = NoAimSlot;
        }

        // 松手：普通卡以锚点（卡根 pivot＝卡下沿中点）是否落进出牌区矩形判定，瞄准卡以"是否指向了某个敌人"（aimSlot ≥ 0）判定；
        // 通过即入队并发出打牌命令，否则回位。松手瞬间已不可打、或这张牌已在拖拽途中离手（被弃置 / 回收）时也不打出——
        // 不把过期命令塞进队列等下一个玩家回合结算
        public void OnCardReleased(UI_CardBattle card_)
        {
            bool aiming = card_ == _aimingCard;
            if (aiming) EndAim(); // 松手即收箭头与瞄准身份

            ClearDragged();
            bool accepted = _cardsPlayable && IsInHand(card_) &&
                            (aiming ? _aimSlot >= 0 : _dropZone.Accepts(card_.WorldPosition));
            if (!accepted)
            {
                ReturnToSlotAsync(card_).Forget();
                return;
            }

            _dropZone.Enqueue(card_); // 收下即挂出：待结算的成员关系由出牌区队列唯一持有
            ApplyLayout(); // 入队即让位：整手立刻合拢，不等域侧回声
            _commandDispatcher.PlayCard(card_.Card, _aimSlot);
        }

        // 拖拽被强制中断（包装禁用或回收）时直接落回槽位，不播补间；瞄准中的牌顺手收掉箭头与瞄准身份
        public void OnCardDragCancelled(UI_CardBattle card_)
        {
            if (card_ == _aimingCard) EndAim();
            ClearDragged();
            PlaceToSlot(card_);
        }

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，首次上报即展开箭头；此后位姿与颜色由每帧驱动刷新
        public void OnAimDragged(UI_CardBattle card_)
        {
            if (card_ != _aimingCard) return;
            _aimingActive = true;
            _cardArrow.Show(card_.WorldCenter, PointerWorldPosition(), _aimSlot >= 0);
        }

        /// <summary>当前瞄准的目标槽位（存活敌人列表下标，-1 = 未指向敌人）：打牌命令与目标提示预览共用这一处来源，由单位侧每帧回写。</summary>
        public int CurrentAimSlot => _aimSlot;

        /// <summary>回写当前瞄准的目标槽位：-1 = 未指向任何敌人，松手时据此取消。</summary>
        public void SetAimSlot(int slot_) => _aimSlot = slot_;

        /// <summary>正在瞄准的牌：拖拽中、本回合可打，且走瞄准模式（本卡目标类型为 AnyEnemy）或锚点（下沿中点）落在出牌区矩形内；没在瞄准时为 null。</summary>
        public UI_CardBattle AimedCard =>
            _draggedCard && _cardsPlayable &&
            (_draggedCard.Card.IsAnyEnemyTarget || _dropZone.Accepts(_draggedCard.WorldPosition))
                ? _draggedCard
                : null;

        // 瞄准的逐帧驱动：卡离手或不再被按住时兜底取消；箭头展开期间刷新位姿与颜色
        private void TickAim()
        {
            if (_aimingCard == null) return;
            if (_draggedCard != _aimingCard || !IsInHand(_aimingCard))
            {
                CancelAim();
                return;
            }

            if (_aimingActive) _cardArrow.Show(_aimingCard.WorldCenter, PointerWorldPosition(), _aimSlot >= 0);
        }

        // 取消瞄准：收箭头、清身份，把牌瞬移回槽位（兜底与状态切换路径，不播补间）
        private void CancelAim()
        {
            if (_aimingCard == null) return;
            UI_CardBattle card = _aimingCard;
            EndAim();
            ClearDragged();
            PlaceToSlot(card);
        }

        // 收瞄准：清身份并收起箭头（箭头对"从未展开"是空操作）
        private void EndAim()
        {
            if (_aimingCard == null) return;
            _aimingCard = null;
            _aimingActive = false;
            _cardArrow.Hide();
        }

        #endregion

        #region 牌的去向：取牌、离手、回收与飞行借还

        // 取出并登记：取池 → 绑卡 → 接入宿主 → 进手牌列 → 归手牌层 → 标记入场中（入场期间不参与悬停，落位才参与）
        private UI_CardBattle Spawn(CardModel model_)
        {
            UI_CardBattle view = _cardPool.Get(_cardPrefab);
            view.Bind(model_);
            view.SetHost(this);
            _views[model_] = view;
            _hand.Add(view);
            MoveToHand(view); // 池里出来的实例可能残留别的父节点与兄弟位，取出即归位
            _entering.Add(view);
            return view;
        }

        // 离手：只从手牌列除名，模型索引仍保留（打出后还要按模型把它找回来）；离手即不参与悬停，行内判定自己会否掉
        private void RemoveFromHand(UI_CardBattle card_) => _hand.Remove(card_);

        // 回收：注销模型索引并归还卡池（离场补间已由调用方等完，回收回调会清干净补间）
        private void ReturnView(CardModel model_, UI_CardBattle view_)
        {
            _views.Remove(model_);
            _cardPool.Release(view_);
        }

        // 借一张飞行用的包装：只取池，不入手牌列与模型索引；牌面沿用上次 Bind 的残留内容，摆位后立刻归还
        private UI_CardBattle RentFlyView()
        {
            UI_CardBattle view = _cardPool.Get(_cardPrefab);
            MoveToFly(view); // 出池即换到飞行层，保证借出去的牌画在队列与手牌之上
            return view;
        }

        private void ReturnFlyView(UI_CardBattle view_) => _cardPool.Release(view_);

        private bool TryGetView(CardModel model_, out UI_CardBattle view_) => _views.TryGetValue(model_, out view_);

        /// <summary>这张包装此刻是否还在手牌列里：拖拽途中被弃置或回收时，用它判"过期的松手"。</summary>
        private bool IsInHand(UI_CardBattle card_) => _hand.Contains(card_);

        // 入场结束：注销入场标记，这张牌此后可以悬停；若此刻正有牌悬停着，顺手把它纳入避让（它右侧的牌已让开）
        private void UnmarkEntering(UI_CardBattle card_)
        {
            _entering.Remove(card_);
            if (_hoveredCard) ApplyHoverDodge();
        }

        #endregion

        #region 资格：可打状态、抓取与悬停

        /// <summary>这张牌此刻参不参与悬停：只看资格，不看动画——滑行中、正要合拢的牌照收；与"本回合可打"无关，不能打的牌照样抬起放大。</summary>
        private bool CanHover(UI_CardBattle card_) =>
            _draggedCard == null && IsInRow(card_) && !_entering.Contains(card_);

        /// <summary>这张牌此刻能不能抓：悬停资格之上再要求本回合可打。</summary>
        public bool CanGrab(UI_CardBattle card_) => _cardsPlayable && CanHover(card_);

        // 拖拽身份：松手与中断时清空（抓起时由 OnCardGrabbed 写入）
        private void ClearDragged() => _draggedCard = null;

        #endregion

        #region 层归属：谁此刻属于哪一层、在层内排第几

        // 手牌层：按行内序号插位（靠右压靠左）；不在行里（已让位、正在回收）只换层
        private void MoveToHand(UI_CardBattle card_) => card_.MoveToLayer(_handLayer, RowIndexOf(card_));

        // 飞行层：离场、洗回与遗忘借道一律追加置末；飞行的牌不再参与悬停
        private void MoveToFly(UI_CardBattle card_) => card_.MoveToLayer(_flyLayer, -1);

        // 交互层：悬停与拖拽一律追加置末，后进来的那张（抓起的）自然压过悬停那张
        private void MoveToFocus(UI_CardBattle card_) => card_.MoveToLayer(_focusLayer, -1);

        #endregion

        #region 槽位与排布：以手牌区的点为准

        // 槽位求解：x 等距张开（间距撑不破可用半宽就保持原间距，撑破则整段压缩），y 落在浅弧上，朝向取切角，
        // 不可打时在 y 上叠下沉量。弧顶（正中那张）恒落在手牌区的点上，故点被拖动、缩放、旋转时整套跟着走，
        // 这里不需要任何基准高度，也不需要知道参差多层的父节点长什么样。
        // dodgeX_ 只平移到最终 x 上：拱高与倾角仍按未避让的槽位 x 算，避让中的半扇手牌是一条纯水平平移、弧不变形
        private void EvaluateSlot(int index_, int count_, float dodgeX_, out Vector3 worldPosition_,
            out Quaternion worldRotation_, out float scale_)
        {
            float gap = count_ <= 1 ? 0f : Mathf.Min(_fanStepPx, 2f * _fanHalfRunPx / (count_ - 1));
            float x = (index_ - (count_ - 1) * 0.5f) * gap;
            // 弧线按可用半宽归一化：正中落在点上（偏移 0），两端比中间低出拱高
            float offset = x / _fanHalfRunPx;
            float y = -_fanSagPx * offset * offset;
            if (!_cardsPlayable) y += UnplayableDropPx;
            worldPosition_ = _fanCenterPoint.TransformPoint(new Vector2(x + dodgeX_, y));
            worldRotation_ = _fanCenterPoint.rotation * Quaternion.Euler(0f, 0f, -_fanTiltMaxDeg * offset);
            scale_ = Mathf.Max(_fanMinScale, 1f - _fanScaleStep * Mathf.Max(0, count_ - _fanFullCount));
        }

        /// <summary>该行内序号的牌此刻的避让量：悬停牌左侧左移、右侧右移各一档，悬停牌本身与无悬停时为 0。</summary>
        private float DodgeOf(int index_)
        {
            if (_hoverDodgePx == 0f || !_hoveredCard) return 0f;
            int hovered = RowIndexOf(_hoveredCard);
            if (hovered < 0) return 0f;
            return index_ > hovered ? _hoverDodgePx : index_ < hovered ? -_hoverDodgePx : 0f;
        }

        // 整体重排：手牌增删、让位与可打状态切换后调用。
        // 行内序号按"行"数（已让位等结算的牌不占号，也不被排布驱动）；入场中、悬停中与被拖拽的牌各有自己的补间，
        // 这里只占位、不写姿态；"要不要重开补间"一律由卡片按通道已声明的目标判（同目标续用、换目标杀旧重开），
        // 故这里只声明目标——判据不能放在这里比当前姿态：同一帧内先让位、随即被驳回退回时补间刚建立、transform 还没挪动，
        // "我马上要离开的位置"会被误判成"已就位"，排布命令就被静默吞掉了
        private void ApplyLayout()
        {
            int count = RowCount();
            int index = 0;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardBattle card = _hand[i];
                if (_dropZone.Contains(card)) continue;

                if (card == _hoveredCard || card == _draggedCard || _entering.Contains(card))
                {
                    index++;
                    continue;
                }

                MoveToHand(card); // 手牌层内按行内序号插位：靠右压靠左（序号没变时一个字都不写）
                EvaluateSlot(index, count, DodgeOf(index), out Vector3 position, out Quaternion rotation,
                    out float scale);
                index++;
                card.MoveToAsync(position, rotation, FanMoveDuration).Forget();
                card.ScaleToAsync(scale, FanMoveDuration).Forget();
            }
        }

        // 补间回槽位：松手未命中出牌区时用，顺手把牌换回手牌层并按行内序号插位
        private UniTask ReturnToSlotAsync(UI_CardBattle card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale))
                return UniTask.CompletedTask;

            MoveToHand(card_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, FanMoveDuration),
                card_.ScaleToAsync(scale, FanMoveDuration));
        }

        // 瞬移回槽位：拖拽被强制中断时用，不播补间
        private void PlaceToSlot(UI_CardBattle card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale)) return;

            MoveToHand(card_);
            card_.Place(position, rotation);
            card_.transform.localScale = Vector3.one * scale;
        }

        // 槽位查询：以行内序号现算槽位，不在行里（已让位等结算 / 已离手）返回 false 由调用方静默
        private bool TryEvaluateSlot(UI_CardBattle card_, out Vector3 worldPosition_, out Quaternion worldRotation_,
            out float scale_)
        {
            int index = RowIndexOf(card_);
            if (index < 0)
            {
                worldPosition_ = default;
                worldRotation_ = default;
                scale_ = 1f;
                return false;
            }

            EvaluateSlot(index, RowCount(), DodgeOf(index), out worldPosition_, out worldRotation_, out scale_);
            return true;
        }

        // 行内：手牌列里还没让位给出牌区等结算的牌；行内序号即槽位序号
        private bool IsInRow(UI_CardBattle card_) => _hand.Contains(card_) && !_dropZone.Contains(card_);

        private int RowCount()
        {
            int count = 0;
            for (int i = 0; i < _hand.Count; i++)
                if (!_dropZone.Contains(_hand[i]))
                    count++;
            return count;
        }

        // 行内序号：不在行里（已让位等结算 / 已离手）返回 -1，由调用方静默
        private int RowIndexOf(UI_CardBattle card_)
        {
            if (!IsInRow(card_)) return -1;
            int index = 0;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardBattle card = _hand[i];
                if (card == card_) break;
                if (!_dropZone.Contains(card)) index++;
            }

            return index;
        }

        #endregion

        #region 指针：坐标换算与每帧裁决

        // 指针在画布平面上的世界坐标：拖拽跟手、箭头指向与落点判定共用。画布是 ScreenSpaceCamera，换算必须传画布相机
        public Vector3 PointerWorldPosition()
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_fanCenterPoint,
                Pointer.current.position.ReadValue(),
                _canvasCamera, out Vector3 world);
            return world;
        }

        // 每帧裁定一次悬停：指针下没有可交互的牌时目标为 null，等价于退出；次序是"先定目标 → 撤旧 → 写新"。
        // 进入姿态一律以槽位为基准（槽位位置 + 沿点朝上的抬起、朝向回正到点的朝向），故滑到一半被悬停也不会停在半途
        private void TickHover()
        {
            UI_CardBattle current = _hoveredCard;
            UI_CardBattle target = RaycastTopCard();
            if (target == current) return;

            _hoveredCard = target; // 先落新悬停身份：撤旧与避让都按它算
            if (current) ExitHover(current);
            if (!target)
            {
                ApplyHoverDodge(); // 悬停撤离：已无悬停者，两侧避让原路收回
                return;
            }

            int index = RowIndexOf(target);
            EvaluateSlot(index, RowCount(), 0f, out Vector3 position, out _, out _);
            MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段交给包装的悬停补间，这里只定基准
            target.EnterHover(position + _fanCenterPoint.TransformVector(Vector3.up * target.HoverLiftPx),
                _fanCenterPoint.rotation);
            ApplyHoverDodge();
        }

        // 词条说明气泡列的对账：悬停牌（且没在拖拽）身上"不展示描述"的词条，在卡片右上角自上而下叠列说明。
        // 整列挂在悬停牌自己名下，随卡抬起、缩放与换层自动跟随，故同一张卡一次写入即够（不再每帧写位置）；
        // 换卡才重建内容并把整列重挂过去，悬停身份被清、开拖、牌离手都落到这里收起
        private void TickAffixTips()
        {
            UI_CardBattle card = _draggedCard ? null : _hoveredCard;
            if (card == _tipCard) return;

            if (_tipCard) _popTextRoot.HideTips(_tipCard);
            _tipCard = card;
            if (!card) return;

            _affixTips.Clear();
            foreach ((string text, CardAffix affix) in card.Card.GetDescriptionLines())
                if (affix != null) _affixTips.Add((text, affix.GetDescription()));
            if (_affixTips.Count > 0) _popTextRoot.ShowTips(card, _affixTips, card.AffixTipAnchor, EUiAnchor.BottomLeft, card.transform);
        }

        // 让出悬停身份：切换不可打、牌离手或被拖走时调用，只清状态与身份，姿态一律交给后续补间
        private void ClearHover()
        {
            if (!_hoveredCard) return;
            _hoveredCard.ClearHover();
            _hoveredCard = null;
            ApplyHoverDodge(); // 悬停被外力撤下：两侧避让先收回（已无悬停者，各牌避让量恒 0），悬停牌自身仍交给后续重排
        }

        // 射线裁决：按命中先后取第一张可悬停的牌；同一张卡可能被多个图形报出，只判一次。
        // 当前悬停中的牌照常作为候选参与（指针还停在它身上就是"目标不变"），因此它的进出补间不会把自己顶掉
        private UI_CardBattle RaycastTopCard()
        {
            if (_hand.Count == 0) return null; // 手牌空时指针下不可能有可悬停的牌，这次射线直接省掉
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return null;

            if (_pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = Pointer.current.position.ReadValue();
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);

            UI_CardBattle previous = null;
            foreach (RaycastResult result in _raycastResults)
            {
                UI_CardBattle card = result.gameObject.GetComponentInParent<UI_CardBattle>();
                if (card == previous) continue; // 同一张卡可能被多个图形报出，只判一次
                previous = card;
                if (!card) continue;
                if (CanHover(card)) return card;
            }

            return null;
        }

        // 撤下悬停：牌还在行里、没被拖拽或让位时，换回手牌层并按当前槽位一次还原位置、朝向与缩放；
        // 其余情况（离手 / 被拖走 / 已让位等结算）只清状态，交给后续补间
        private void ExitHover(UI_CardBattle card_)
        {
            if (!CanHover(card_))
            {
                card_.ClearHover();
                return;
            }

            int index = RowIndexOf(card_);
            MoveToHand(card_);
            EvaluateSlot(index, RowCount(), DodgeOf(index), out Vector3 position, out Quaternion rotation,
                out float scale);
            card_.ExitHover(position, rotation, scale);
        }

        // 悬停避让：以悬停牌为界，左半行内牌左移、右半右移各一档（无悬停者时各牌避让量恒 0，原路归位）；
        // 避让量口径与 DodgeOf 同一处（行内序号现算，悬停牌自身不进循环）；补间与悬停进出同时长——避让是悬停演出的一部分，要跟手。
        // 只挪位置，不碰缩放与层归属；同样只声明目标，"要不要重开补间"由卡片按已声明的目标判（与手牌重排共用同一套通道）
        private void ApplyHoverDodge()
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardBattle card = _hand[i];
                if (card == _hoveredCard || card == _draggedCard || _entering.Contains(card)) continue;
                if (!IsInRow(card)) continue;

                int index = RowIndexOf(card);
                EvaluateSlot(index, RowCount(), DodgeOf(index), out Vector3 position, out Quaternion rotation, out _);
                card.MoveToAsync(position, rotation, card.HoverDuration).Forget();
            }
        }

        #endregion

        #region 领域事件演出：十条订阅与全部演出

        // 洗回搬运：域侧一次搬完整摞，这里演成整波同飞——洗回多少张就飞多少张，一张飞抵即一张进堆。
        // 借来的包装只取池、不入手牌列与模型索引，牌面沿用上次 Bind 的残留内容（整段飞行都缩得很小，不为它单独刷外观）。
        // 起落点按对称铺开骨架错开保底不重叠，再叠随机抖动打散轨迹；域侧逐事件 await，这段播完才轮到本次抽牌入场
        private async UniTask OnDiscardReshuffledAsync(BE_DiscardReshuffled reshuffled_)
        {
            UI_CardBattle[] cards = new UI_CardBattle[reshuffled_.Count];
            Vector3 from = discardPile.transform.position;
            Vector3 to = drawPile.transform.position;

            // 起飞：逐张摆在弃牌堆附近的散开点上，缩到飞行用的起始尺寸
            for (int i = 0; i < reshuffled_.Count; i++)
            {
                UI_CardBattle card = RentFlyView();
                cards[i] = card;
                card.Place(from + FlyOffset(i, reshuffled_.Count) + Jitter(discardPile.transform),
                    discardPile.transform.rotation);
                card.transform.localScale = Vector3.one * ReshuffleFlyScale;
            }

            // 同帧全部起飞，一张落地就通知抽牌堆进了一张（数字随之加并弹一下）
            UniTask[] flights = new UniTask[reshuffled_.Count];
            for (int i = 0; i < reshuffled_.Count; i++)
            {
                UI_CardBattle card = cards[i];
                discardPile.OnCardLeft(); // 起飞即通知弃牌堆"牌离堆"，数字随之减
                flights[i] = FlyToDrawPileAsync(card,
                    to + FlyOffset(i, reshuffled_.Count) + Jitter(drawPile.transform));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < reshuffled_.Count; i++) ReturnFlyView(cards[i]);

            async UniTask FlyToDrawPileAsync(UI_CardBattle card_, Vector3 destination_)
            {
                await FlyOutAsync(card_, destination_, drawPile.transform.rotation, ReshuffleFlyDuration);
                drawPile.OnCardArrived();
            }
        }

        // 飞行牌的起落点错开量：横向按序号对称铺开、纵向奇偶反向；整串牌的总跨度不超过上限，张数越多每张越挤。
        // 幅度按画布像素给，再按抽牌堆节点的 画布像素→世界 换算成世界向量
        private Vector3 FlyOffset(int index_, int count_)
        {
            float spread = count_ <= 1 ? 0f : Mathf.Min(ReshuffleFlySpreadPx, ReshuffleFlySpreadMaxPx / (count_ - 1));
            float lateral = (index_ - (count_ - 1) * 0.5f) * spread;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * spread * 0.5f;
            return drawPile.transform.TransformVector(new Vector2(lateral, vertical));
        }

        // 起落点的随机抖动：±幅度内均匀分布的画布像素，经节点换算成世界向量，只打散重叠轨迹、不改朝向
        private Vector3 Jitter(Transform node_) =>
            node_.TransformVector(new Vector2(_rng.Next(-2f, 2f), _rng.Next(-2f, 2f)) * FlyJitterPx);

        // 飞到目标姿态并缩到 0：三处归宿飞行（离场、弃牌波、洗回）共用的成品段落；起落点的抖动与进堆通知留在各自调用点
        private UniTask FlyOutAsync(UI_CardBattle view_, Vector3 position_, Quaternion rotation_, float duration_) =>
            UniTask.WhenAll(
                view_.MoveToAsync(position_, rotation_, duration_),
                view_.ScaleToAsync(0f, duration_));

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 槽位与峰值都按现算：手牌超过不缩小的张数上限后每张都有缩放，新牌不能按常态 1 落位，否则比同手牌大一截、能点的范围也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            drawPile.OnCardLeft(); // 牌从抽牌堆起飞去手牌：先记离堆，数字在入场飞行之前就减
            CardModel model = drawn_.Card;
            UI_CardBattle view = Spawn(model);
            view.SetPlayableLook(_cardsPlayable); // 将来敌方回合塞牌也不会漏刷
            int count = RowCount();
            EvaluateSlot(count - 1, count, DodgeOf(count - 1),
                out Vector3 position, out Quaternion rotation, out float slotScale);
            view.Place(drawPile.transform.position, drawPile.transform.rotation);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次（新牌入场中，重排会跳过它）
            ApplyLayout();
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * DrawGrowOvershoot, DrawGrowDuration),
                view.MoveToAsync(position, rotation, DrawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!TryGetView(model, out UI_CardBattle bound) || bound != view)
            {
                UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(slotScale, DrawSettleDuration);
            UnmarkEntering(view);
            ApplyLayout(); // 入场期间行若有过变动（别的牌打出 / 让位），在这里一次性纠正到最新槽位
        }

        // 轮到执行：域侧确认这张已离手（手牌成员关系到此注销），行早在松手那一刻就让过位，这里只等入队补间播完
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!TryGetView(played_.Card, out UI_CardBattle view)) return;
            RemoveFromHand(view);
            ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并整手重排一次——行重新开缝，这张从队列位置飞回自己的槽位
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (!TryGetView(rejected_.Card, out UI_CardBattle view)) return UniTask.CompletedTask;
            if (!_dropZone.Remove(view)) return UniTask.CompletedTask;
            ApplyLayout();
            return UniTask.CompletedTask;
        }

        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌，全部出队后整手重排一次——行重新开缝，退回的牌与其余牌一起归位
        private void ReconcilePending()
        {
            IReadOnlyList<UI_CardBattle> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            List<UI_CardBattle> snapshot = new(queued); // 出队会改动原队列，先拷一份再退
            foreach (UI_CardBattle card in snapshot) _dropZone.Remove(card);
            ApplyLayout();
        }

        // 玩家回合结束：进入弃牌演出即落闸——手牌下沉置灰、取消瞄准，不再接受拖拽与出牌；
        // 开闸的唯一时机仍是下个玩家回合开始的 BE_TurnStarted，中间整段敌方回合都保持落闸
        private UniTask OnPlayerTurnEndedAsync(BE_PlayerTurnEnded ended_)
        {
            ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再对账退回仍挂着等结算的牌，
        // 最后把三堆数字对齐域侧活值兜底——万有一条进堆 / 离堆通知没发到，最迟在这一刻纠正
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ApplyPlayable(started_.IsPlayerTurn);
            ReconcilePending();
            drawPile.SyncToLive();
            discardPile.SyncToLive();
            exhaustPile.SyncToLive();
            return UniTask.CompletedTask;
        }

        // 战斗结束：不再有回合交替，手牌一律收起为不可打
        private UniTask OnBattleEndedAsync(BE_BattleEnded ended_)
        {
            ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 可打状态切换：先取消瞄准并让出悬停身份（不可打后就不再可交互，ExitHover 只会清状态、会留下半截放大），
        // 再按槽位整体重排（位置、缩放与所在层一起回位），最后统一刷整手牌外观；状态没变则什么都不动
        private void ApplyPlayable(bool playable_)
        {
            if (_cardsPlayable == playable_) return;
            _cardsPlayable = playable_;
            if (!playable_)
            {
                CancelAim();
                ClearHover();
            }

            ApplyLayout();
            foreach (UI_CardBattle card in _hand) card.SetPlayableLook(playable_);
        }

        // 批量弃牌波：整手未保留的牌同帧各自飞向弃牌堆并缩到 0，全部落地才算波次播完（其后域侧才推进到状态衰减与敌方回合）。
        // 起飞逐张记离堆（数字一次掉到底），落地逐张记进堆（数字随落地递增）；起落位各带随机抖动打散重叠轨迹
        private async UniTask OnCardsDiscardedAsync(BE_CardsDiscarded discarded_)
        {
            Transform target = discardPile.transform;
            List<UniTask> flights = new();

            foreach (CardModel model in discarded_.Cards)
            {
                if (!TryGetView(model, out UI_CardBattle view)) continue;
                RemoveFromHand(view);
                _dropZone.Remove(view);
                view.Place(view.WorldPosition + Jitter(target), view.WorldRotation);
                MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
                discardPile.OnCardLeft();
                flights.Add(FlyToDiscardPileAsync(view, model, target));
            }

            if (flights.Count == 0) return;
            ApplyLayout();

            await UniTask.WhenAll(flights);

            // 波内单张归宿飞行：飞向牌堆并缩到 0，落定即通知进堆、注销索引并回收进池
            async UniTask FlyToDiscardPileAsync(UI_CardBattle view_, CardModel model_, Transform target_)
            {
                await FlyOutAsync(view_, target_.position + Jitter(target_), target_.rotation, ExitDuration);
                discardPile.OnCardArrived();
                ReturnView(model_, view_);
            }
        }

        // 归宿：离手 → 出队 → 整体重排 → 飞向目标牌堆（传 null 即飞往出牌区）并缩到 0 → 落地通知目标堆进了一张 → 注销索引、回收进池。
        // 进堆通知只在落地这一帧发出，故牌堆的数字与弹跳与飞行同步；目标为 null（遗忘借道）时没有"进堆"这回事，不发通知
        private async UniTask ExitToPileAsync(CardModel model_, UI_Pile arrival_)
        {
            if (!TryGetView(model_, out UI_CardBattle view)) return;
            RemoveFromHand(view);
            _dropZone.Remove(view);
            ApplyLayout();

            Transform target = arrival_ != null ? arrival_.transform : _dropZone.transform;
            MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
            await FlyOutAsync(view, target.position, target.rotation, ExitDuration);

            if (arrival_ != null) arrival_.OnCardArrived();
            ReturnView(model_, view);
        }

        #endregion
    }
}