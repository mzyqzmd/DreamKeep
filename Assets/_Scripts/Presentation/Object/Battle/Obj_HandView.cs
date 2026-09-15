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
    /// <summary>手牌视图：手牌与出牌全流程的编排者——按领域事件生成与回收卡面、播放抽牌入场与归宿演出、沿曲线排布手牌、把拖拽落点转成打出命令。
    /// 只发命令与消费事件，不判断任何规则：能量够不够、目标合不合法一律由域侧校验，被丢弃的命令由阶段对账兜底。</summary>
    public class Obj_HandView : MonoBehaviour
    {
        private const int DefaultAimSlot = 0;

        [SerializeField] private Obj_Card _cardPrefab;
        [SerializeField] private SplineContainer _handSpline;
        [SerializeField] private Obj_PlayCardZone[] _dropZones;
        [SerializeField] private Obj_DrawPileView _drawPileView;
        [SerializeField] private Obj_DiscardPileView _discardPileView;
        [SerializeField] private Obj_ExhaustPileView _exhaustPileView;

        [Header("抽牌入场")]
        [SerializeField] private float _drawGrowDuration = 0.25f;
        [SerializeField] private float _drawGrowOvershoot = 1.08f;
        [SerializeField] private float _drawGrowAlpha = 0.9f;
        [SerializeField] private float _drawSettleDuration = 0.12f;

        [Header("手牌排布")]
        [SerializeField] private float _fanStartT = 0.06f;
        [SerializeField] private float _fanEndT = 0.94f;
        [SerializeField] private float _fanMoveDuration = 0.28f;
        [SerializeField] private int _fanFullCount = 5;
        [SerializeField] private float _fanScaleStep = 0.06f;
        [SerializeField] private float _fanMinScale = 0.7f;
        [SerializeField] private int _handBaseOrder = 10;

        [Header("归宿离场")]
        [SerializeField] private float _exitDuration = 0.3f;
        [SerializeField] private int _exitOrder = 80;

        private readonly List<Obj_Card> _hand = new();
        private readonly Dictionary<CardModel, Obj_Card> _views = new();
        private readonly HashSet<Obj_Card> _pendingPlays = new();

        private ObjectPool<Obj_Card> _cardPool;
        private Obj_Card _draggedCard;
        private BattleCommandDispatcher _commandDispatcher;

        private void Awake()
        {
            _cardPool = new ObjectPool<Obj_Card>(
                () => Instantiate(_cardPrefab, transform),
                actionOnGet: view_ => view_.gameObject.SetActive(true),
                actionOnRelease: view_ =>
                {
                    view_.KillMotion();
                    view_.gameObject.SetActive(false);
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

        // 松手：落在出牌区即视为选定目标打出，否则回到手牌槽位
        public void OnCardReleased(Obj_Card card_, Vector3 worldPosition_)
        {
            _draggedCard = null;
            Obj_PlayCardZone zone = FindZone(worldPosition_);
            if (zone == null)
            {
                ReturnToHandAsync(card_).Forget();
                return;
            }

            PlayToZone(card_, zone);
        }

        // 拖拽被强制中断（组件禁用或回收）时直接落回槽位，不播补间
        public void OnCardDragCancelled(Obj_Card card_)
        {
            _draggedCard = null;
            PlaceToHandSlot(card_);
        }

        #endregion

        // 抽牌入场：池中取出 → 抽牌堆位置零尺寸全透明 → 放大淡入并滑翔到手牌槽位 → 落位后收正尺寸补完淡入
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            CardModel model = drawn_.Card;
            Obj_Card view = _cardPool.Get();
            view.Bind(model);
            view.SetHost(this);
            view.AttachAbility<Comp_CardDrag>();
            _views[model] = view;
            _hand.Add(view);

            EvaluateFanSlot(_hand.Count - 1, _hand.Count, out Vector3 position, out Quaternion rotation, out _, out int order);
            view.Place(_drawPileView.Anchor, Quaternion.identity, order);
            view.SetScale(0f);
            view.SetAlpha(0f);

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            await UniTask.WhenAll(
                view.ScaleToAsync(_drawGrowOvershoot, _drawGrowDuration),
                view.FadeToAsync(_drawGrowAlpha, _drawGrowDuration),
                view.MoveToAsync(position, rotation, order, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_views.TryGetValue(model, out Obj_Card bound) || bound != view) return;

            await UniTask.WhenAll(
                view.ScaleToAsync(1f, _drawSettleDuration),
                view.FadeToAsync(1f, _drawSettleDuration));

            ApplyFanLayout();
        }

        // 轮到执行：卡已离手并停在出牌区，等入队补间播完即可，结算期间不飞不缩
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_views.TryGetValue(played_.Card, out Obj_Card view)) return;
            _pendingPlays.Remove(view);
            _hand.Remove(view);
            ApplyFanLayout();

            Obj_PlayCardZone zone = FindZoneOf(view);
            if (zone == null) return;
            await zone.WaitArrivalAsync(view);
        }

        private UniTask OnCardDiscardedAsync(BE_CardDiscarded discarded_) => ExitToPileAsync(discarded_.Card, _discardPileView.Anchor);

        private UniTask OnCardExhaustedAsync(BE_CardExhausted exhausted_) => ExitToPileAsync(exhausted_.Card, _exhaustPileView.Anchor);

        // 遗忘按既定口径同样飞往消耗堆，此处只播表现形式，永久移除与否由域侧决定
        private UniTask OnCardErasedAsync(BE_CardErased erased_) => ExitToPileAsync(erased_.Card, _exhaustPileView.Anchor);

        // 阶段对账：域侧对能量不足、非玩家回合等命令静默丢弃且不发事件，阶段一变就把悬着的出牌请求退回手牌
        private UniTask OnTurnPhaseChangedAsync(BE_TurnPhaseChanged changed_)
        {
            if (_pendingPlays.Count == 0) return UniTask.CompletedTask;

            List<Obj_Card> rejected = new List<Obj_Card>(_pendingPlays);
            _pendingPlays.Clear();
            foreach (Obj_Card card in rejected)
            {
                FindZoneOf(card)?.Remove(card);
                ReturnToHandAsync(card).Forget();
            }

            return UniTask.CompletedTask;
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆并缩到 0 → 回收进池
        private async UniTask ExitToPileAsync(CardModel model_, Vector3 destination_)
        {
            if (!_views.TryGetValue(model_, out Obj_Card view)) return;
            _pendingPlays.Remove(view);
            _hand.Remove(view);
            FindZoneOf(view)?.Remove(view);
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

        // 手牌变化即重排：拖拽中的牌与已挂出牌的牌不参与
        private void ApplyFanLayout()
        {
            int count = _hand.Count;
            for (int i = 0; i < count; i++)
            {
                Obj_Card card = _hand[i];
                if (card == _draggedCard || _pendingPlays.Contains(card)) continue;

                EvaluateFanSlot(i, count, out Vector3 position, out Quaternion rotation, out float scale, out int order);
                card.MoveToAsync(position, rotation, order, _fanMoveDuration).Forget();
                card.ScaleToAsync(scale, _fanMoveDuration).Forget();
            }
        }

        // 第 index 张（共 count 张）在弧线上的位置、朝向、大小与层序
        private void EvaluateFanSlot(int index_, int count_, out Vector3 position_, out Quaternion rotation_, out float scale_, out int order_)
        {
            float t = count_ <= 1 ? (_fanStartT + _fanEndT) * 0.5f : Mathf.Lerp(_fanStartT, _fanEndT, (float)index_ / (count_ - 1));
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

            EvaluateFanSlot(index, _hand.Count, out Vector3 position, out Quaternion rotation, out float scale, out int order);
            card_.Place(position, rotation, order);
            card_.SetScale(scale);
        }

        private UniTask ReturnToHandAsync(Obj_Card card_)
        {
            int index = _hand.IndexOf(card_);
            if (index < 0) return UniTask.CompletedTask;

            EvaluateFanSlot(index, _hand.Count, out Vector3 position, out Quaternion rotation, out float scale, out int order);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, order, _fanMoveDuration),
                card_.ScaleToAsync(scale, _fanMoveDuration));
        }

        private Obj_PlayCardZone FindZone(Vector3 worldPosition_)
        {
            for (int i = 0; i < _dropZones.Length; i++)
                if (_dropZones[i].Accepts(worldPosition_)) return _dropZones[i];
            return null;
        }

        private Obj_PlayCardZone FindZoneOf(Obj_Card card_)
        {
            for (int i = 0; i < _dropZones.Length; i++)
                if (_dropZones[i].Contains(card_)) return _dropZones[i];
            return null;
        }
    }
}
