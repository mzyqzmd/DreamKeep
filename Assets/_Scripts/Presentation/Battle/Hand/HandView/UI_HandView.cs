using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌区本体：一块自洽的手牌区——卡池与牌的去向、以手牌区的点为准的排布、拖拽与瞄准、出牌受理，
    /// 并对外只暴露宿主契约：卡牌问抓取资格与指针坐标并上报指针事件，目标提示驱动读瞄准事实并回写目标槽位。
    /// 每帧的次序由这里的 Update 集中驱动（先悬停裁决、再瞄准、后词条气泡）；悬停裁决与避让长在同节点的 UI_HandHover、
    /// 十一条领域事件的演出长在同节点的 UI_HandShow，两者经本类的内部服务面读写这里的状态。</summary>
    [RequireComponent(typeof(UI_HandHover))]
    [RequireComponent(typeof(UI_HandShow))]
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

        [Header("卡牌层")] [Tooltip("飞行层：离场与遗忘借道飞行的牌")] [SerializeField]
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

        [Tooltip("手牌重排（让位、回位）的补间时长（秒）")] [SerializeField]
        private float _fanMoveDuration = 0.2f;

        [Tooltip("非我方回合整手牌的下沉量（画布像素）：负值向下")] [SerializeField]
        private float _unplayableDropPx = -110f;

        /// <summary>非瞄准卡的默认目标槽位：按存活列表最前一个结算（前敌、全体、随机等落点解析不读它）。</summary>
        public const int DefaultAimSlot = 0;

        private const int NoAimSlot = -1; // 尚未指向任何敌人：松手时据此取消，不打出

        // 域侧协作者、卡池与手牌台账
        private BattleCommandDispatcher _commandDispatcher;
        private BattleQueryDispatcher _queryDispatcher;
        private Camera _canvasCamera;
        private MonoObjectPool<UI_CardBattle> _cardPool;
        private readonly List<UI_CardBattle> _hand = new();
        private readonly Dictionary<CardModel, UI_CardBattle> _views = new();
        private readonly HashSet<UI_CardBattle> _entering = new();

        // 交互状态位：本回合可打、正被拖拽；瞄准状态（哪张牌、箭头是否已显示）只在这里，
        // "按下走哪种交互"由包装读自己的卡回答，不经此路
        private bool _cardsPlayable;
        private UI_CardBattle _draggedCard;
        private UI_CardBattle _aimingCard;
        private bool _aimingActive;

        // 当前指向的目标槽位（存活敌人列表下标）：由单位侧的目标提示驱动每帧回写，打牌命令与箭头命中色共用这一处；
        // 瞄准卡一按下先清成"未指向"，防极快操作下默认值被当成有效目标打出去
        private int _aimSlot = DefaultAimSlot;

        // 当前瞄准牌的目标集合（由单位侧的目标提示驱动每帧回传）：数值预览的目标侧修正只取"唯一目标"那一档
        private IReadOnlyList<Creature> _previewTargets;

        // 同节点的悬停器：悬停身份、避让量与词条气泡都长在它身上，这里只留引用与调用（RequireComponent 保证它必有）
        private UI_HandHover _hover;

        // 生命周期：取悬停器引用（与本体同节点，缺失会在首次驱动处直接报出来）
        private void Awake() => _hover = GetComponent<UI_HandHover>();

        // 每帧驱动：先让悬停器裁决悬停进出，再刷新瞄准中的箭头并兜底校正瞄准状态，后按拖拽牌（优先）与悬停牌对账词条说明气泡列，最后对账整手牌的数值预览
        private void Update()
        {
            _hover.TickHover();
            TickAim();
            _hover.TickAffixTips();
            TickPreview();
        }

        // 装配：解析画布相机、建卡池；两个上行门面分别供出牌命令与数值预览查询。画布相机在这时解析（不靠 Awake，避免与容器装配的先后次序打架）；
        // 十一条领域事件的订阅在演出器、悬停裁决在悬停器，各自在他们的装配处
        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleQueryDispatcher queryDispatcher_,
            IObjectResolver resolver_)
        {
            _commandDispatcher = commandDispatcher_;
            _queryDispatcher = queryDispatcher_;
            _canvasCamera = GetComponentInParent<Canvas>().worldCamera;
            // 出生父节点是手牌层；池上限要容得下"手牌 + 一整摞飞行牌"；失活由池内化，复位回调只写卡自己与宿主这边要收的东西；
            // 新建实例只在这时补一次容器注入（卡上的气泡发起器等依赖），池内复用不重复注入
            _cardPool = new MonoObjectPool<UI_CardBattle>(_handLayer, 64,
                onCreate_: view_ => resolver_.InjectGameObject(view_.gameObject),
                onReturn_: view_ =>
                {
                    view_.KillMotion();
                    _entering.Remove(view_); // 回收即注销入场标记与交互身份，防实例复用后残留旧状态
                    _hover.OnViewReleased(view_);
                    if (_draggedCard == view_) _draggedCard = null;
                });
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

        /// <summary>回传当前瞄准牌的目标集合（单位侧的目标提示驱动每帧调用；未瞄准或未解析出目标时传 null）。</summary>
        public void SetPreviewTargets(IReadOnlyList<Creature> targets_) => _previewTargets = targets_;

        /// <summary>正在瞄准的牌：拖拽中、本回合可打，且走瞄准模式（本卡目标类型为 AnyEnemy）或锚点（下沿中点）落在出牌区矩形内；没在瞄准时为 null。</summary>
        public UI_CardBattle AimedCard =>
            _draggedCard && _cardsPlayable &&
            (_draggedCard.Card.IsAnyEnemyTarget || _dropZone.Accepts(_draggedCard.WorldPosition))
                ? _draggedCard
                : null;

        // 数值预览的逐帧驱动：整手每帧对账——平时只含来源侧修正（力量/虚弱），正在瞄准且已指向目标的牌叠加唯一目标的目标侧修正
        //（多目标/随机卡的候选不止一个时不叠加）；未变的卡在卡视图内部差分早退，零字符串构建
        private void TickPreview()
        {
            UI_CardBattle aimed = AimedCard;
            Creature target = aimed != null && _aimSlot >= 0 && _previewTargets != null && _previewTargets.Count == 1
                ? _previewTargets[0]
                : null;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardBattle card = _hand[i];
                card.RefreshPreview(_queryDispatcher, card == aimed ? target : null);
            }
        }

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

        #region 内部服务面：同节点的悬停器与演出器取用

        // 手牌列（只读）：悬停器射线前的空判与避让遍历用
        internal IReadOnlyList<UI_CardBattle> Hand => _hand;

        // 正被拖拽的那张：悬停器避让与词条气泡对账要跳过 / 优先它
        internal UI_CardBattle DraggedCard => _draggedCard;

        // 本回合可不可打：演出器抽牌入场时按它刷一次外观
        internal bool CardsPlayable => _cardsPlayable;

        // 手牌区的点：悬停器换算朝向，指针锚点位也按它换算
        internal RectTransform FanCenterPoint => _fanCenterPoint;

        // 出牌区与三堆：演出器的队位等待、出队、归宿飞行与数字兜底用
        internal UI_PlayCardZone DropZone => _dropZone;
        internal UI_DrawPile DrawPile => drawPile;
        internal UI_DiscardPile DiscardPile => discardPile;
        internal UI_ExhaustPile ExhaustPile => exhaustPile;

        // 入场中：悬停器避让要跳过正在入场的牌
        internal bool IsEntering(UI_CardBattle card_) => _entering.Contains(card_);

        #endregion

        #region 牌的去向：取牌、离手与回收

        // 取出并登记：取池 → 绑卡 → 接入宿主 → 进手牌列 → 归手牌层 → 标记入场中（入场期间不参与悬停，落位才参与）
        internal UI_CardBattle Spawn(CardModel model_)
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
        internal void RemoveFromHand(UI_CardBattle card_) => _hand.Remove(card_);

        // 回收：注销模型索引并归还卡池（离场补间已由调用方等完，回收回调会清干净补间）
        internal void ReturnView(CardModel model_, UI_CardBattle view_)
        {
            _views.Remove(model_);
            _cardPool.Release(view_);
        }

        internal bool TryGetView(CardModel model_, out UI_CardBattle view_) => _views.TryGetValue(model_, out view_);

        /// <summary>这张包装此刻是否还在手牌列里：拖拽途中被弃置或回收时，用它判"过期的松手"。</summary>
        private bool IsInHand(UI_CardBattle card_) => _hand.Contains(card_);

        // 入场结束：注销入场标记，这张牌此后可以悬停；若此刻正有牌悬停着，通知悬停器把它纳入避让（它两侧的行已让开）
        internal void UnmarkEntering(UI_CardBattle card_)
        {
            _entering.Remove(card_);
            _hover.OnCardSettled();
        }

        #endregion

        #region 资格：可打状态、抓取与悬停

        /// <summary>这张牌此刻参不参与悬停：只看资格，不看动画——滑行中、正要合拢的牌照收；与"本回合可打"无关，不能打的牌照样抬起放大。</summary>
        internal bool CanHover(UI_CardBattle card_) =>
            !_draggedCard && IsInRow(card_) && !_entering.Contains(card_);

        /// <summary>这张牌此刻能不能抓：悬停资格之上再要求本回合可打。</summary>
        public bool CanGrab(UI_CardBattle card_) => _cardsPlayable && CanHover(card_);

        // 拖拽身份：松手与中断时清空（抓起时由 OnCardGrabbed 写入）
        private void ClearDragged() => _draggedCard = null;

        #endregion

        #region 层归属：谁此刻属于哪一层、在层内排第几

        // 手牌层：按行内序号插位（靠右压靠左）；不在行里（已让位、正在回收）只换层
        internal void MoveToHand(UI_CardBattle card_) => card_.MoveToLayer(_handLayer, RowIndexOf(card_));

        // 飞行层：离场与遗忘借道一律追加置末；飞行的牌不再参与悬停
        internal void MoveToFly(UI_CardBattle card_) => card_.MoveToLayer(_flyLayer, -1);

        // 交互层：悬停与拖拽一律追加置末，后进来的那张（抓起的）自然压过悬停那张
        internal void MoveToFocus(UI_CardBattle card_) => card_.MoveToLayer(_focusLayer, -1);

        #endregion

        #region 槽位与排布：以手牌区的点为准

        // 槽位求解：x 等距张开（间距撑不破可用半宽就保持原间距，撑破则整段压缩），y 落在浅弧上，朝向取切角，
        // 不可打时在 y 上叠下沉量。弧顶（正中那张）恒落在手牌区的点上，故点被拖动、缩放、旋转时整套跟着走，
        // 这里不需要任何基准高度，也不需要知道参差多层的父节点长什么样。
        // dodgeX_ 只平移到最终 x 上：拱高与倾角仍按未避让的槽位 x 算，避让中的半扇手牌是一条纯水平平移、弧不变形
        internal void EvaluateSlot(int index_, int count_, float dodgeX_, out Vector3 worldPosition_,
            out Quaternion worldRotation_, out float scale_)
        {
            float gap = count_ <= 1 ? 0f : Mathf.Min(_fanStepPx, 2f * _fanHalfRunPx / (count_ - 1));
            float x = (index_ - (count_ - 1) * 0.5f) * gap;
            // 弧线按可用半宽归一化：正中落在点上（偏移 0），两端比中间低出拱高
            float offset = x / _fanHalfRunPx;
            float y = -_fanSagPx * offset * offset;
            if (!_cardsPlayable) y += _unplayableDropPx;
            worldPosition_ = _fanCenterPoint.TransformPoint(new Vector2(x + dodgeX_, y));
            worldRotation_ = _fanCenterPoint.rotation * Quaternion.Euler(0f, 0f, -_fanTiltMaxDeg * offset);
            scale_ = Mathf.Max(_fanMinScale, 1f - _fanScaleStep * Mathf.Max(0, count_ - _fanFullCount));
        }

        // 整体重排：手牌增删、让位与可打状态切换后调用。
        // 行内序号按"行"数（已让位等结算的牌不占号，也不被排布驱动）；入场中、悬停中与被拖拽的牌各有自己的补间，
        // 这里只占位、不写姿态；"要不要重开补间"一律由卡片按通道已声明的目标判（同目标续用、换目标杀旧重开），
        // 故这里只声明目标——判据不能放在这里比当前姿态：同一帧内先让位、随即被驳回退回时补间刚建立、transform 还没挪动，
        // "我马上要离开的位置"会被误判成"已就位"，排布命令就被静默吞掉了
        internal void ApplyLayout()
        {
            int count = RowCount();
            int index = 0;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardBattle card = _hand[i];
                if (_dropZone.Contains(card)) continue;

                if (_hover.IsHovered(card) || card == _draggedCard || _entering.Contains(card))
                {
                    index++;
                    continue;
                }

                MoveToHand(card); // 手牌层内按行内序号插位：靠右压靠左（序号没变时一个字都不写）
                EvaluateSlot(index, count, _hover.DodgeOf(index), out Vector3 position, out Quaternion rotation,
                    out float scale);
                index++;
                card.MoveToAsync(position, rotation, _fanMoveDuration).Forget();
                card.ScaleToAsync(scale, _fanMoveDuration).Forget();
            }
        }

        // 补间回槽位：松手未命中出牌区时用，顺手把牌换回手牌层并按行内序号插位
        private UniTask ReturnToSlotAsync(UI_CardBattle card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale))
                return UniTask.CompletedTask;

            MoveToHand(card_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, _fanMoveDuration),
                card_.ScaleToAsync(scale, _fanMoveDuration));
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
        internal bool TryEvaluateSlot(UI_CardBattle card_, out Vector3 worldPosition_, out Quaternion worldRotation_,
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

            EvaluateSlot(index, RowCount(), _hover.DodgeOf(index), out worldPosition_, out worldRotation_, out scale_);
            return true;
        }

        // 行内：手牌列里还没让位给出牌区等结算的牌；行内序号即槽位序号
        internal bool IsInRow(UI_CardBattle card_) => _hand.Contains(card_) && !_dropZone.Contains(card_);

        internal int RowCount()
        {
            int count = 0;
            for (int i = 0; i < _hand.Count; i++)
                if (!_dropZone.Contains(_hand[i]))
                    count++;
            return count;
        }

        // 行内序号：不在行里（已让位等结算 / 已离手）返回 -1，由调用方静默
        internal int RowIndexOf(UI_CardBattle card_)
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

        #region 指针：坐标换算

        // 指针在画布平面上的世界坐标：拖拽跟随、箭头指向与落点判定共用。画布是 ScreenSpaceCamera，换算必须传画布相机
        public Vector3 PointerWorldPosition()
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_fanCenterPoint,
                Pointer.current.position.ReadValue(),
                _canvasCamera, out Vector3 world);
            return world;
        }

        // 指针锚点位：让"卡锚点（下沿中点）上方 AnchorLiftPx 的点"与指针重合时，卡应处的世界位置——拖拽跟手专用，
        // 悬停抬起不读指针（按手牌区的点换算，与卡自身缩放无关，故拖动起始无跳变）
        internal Vector3 PointerAnchorPosition(UI_CardBattle card_) =>
            PointerWorldPosition() - _fanCenterPoint.TransformVector(Vector3.up * card_.AnchorLiftPx);

        #endregion

        #region 闸门与对账：演出器按域事件调用

        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌，全部出队后整手重排一次——行重新开缝，退回的牌与其余牌一起归位
        internal void ReconcilePending()
        {
            IReadOnlyList<UI_CardBattle> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            List<UI_CardBattle> snapshot = new(queued); // 出队会改动原队列，先拷一份再退
            foreach (UI_CardBattle card in snapshot) _dropZone.Remove(card);
            ApplyLayout();
        }

        // 可打状态切换：先取消瞄准并让出悬停身份（不可打后就不再可交互，ExitHover 只会清状态、会留下半截放大），
        // 再按槽位整体重排（位置、缩放与所在层一起回位），最后统一刷整手牌外观；状态没变则什么都不动
        internal void ApplyPlayable(bool playable_)
        {
            if (_cardsPlayable == playable_) return;
            _cardsPlayable = playable_;
            if (!playable_)
            {
                CancelAim();
                _hover.ClearHover();
            }

            ApplyLayout();
            foreach (UI_CardBattle card in _hand) card.SetPlayableLook(playable_);
        }

        #endregion
    }
}