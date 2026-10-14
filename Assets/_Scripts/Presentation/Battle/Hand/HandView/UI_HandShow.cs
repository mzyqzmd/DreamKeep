using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌演出器：与手牌区本体同挂一个节点，订阅全部十一条领域事件，把域侧的决定演成手牌的动作——
    /// 抽牌入场、打出离场、被拒退回、批量弃牌波、离场与遗忘飞行、洗回整波，以及回合开闸/落闸与战斗结束收起。
    /// 它只编排演出与通知牌堆，手牌区的状态、布局与卡池一律经本体的内部服务面代劳；演出与手感的时长幅度都在 Inspector 上可调。</summary>
    public sealed class UI_HandShow : MonoBehaviour
    {
        [Header("抽牌入场")]
        [Tooltip("抽牌飞行时长（秒）：从抽牌堆零尺寸滑到手牌槽位")] [SerializeField]
        private float _drawGrowDuration = 0.2f;

        [Tooltip("抽牌途中的放大倍率：落位后再收回槽位尺寸")] [SerializeField]
        private float _drawGrowOvershoot = 1.1f;

        [Tooltip("飞行到位后收回常态尺寸的时长（秒）")] [SerializeField]
        private float _drawSettleDuration = 0.15f;

        [Header("离场")]
        [Tooltip("离场飞向牌堆并缩到 0 的时长（秒）")] [SerializeField]
        private float _exitDuration = 0.22f;

        [Header("洗回演出")]
        [Tooltip("单张洗回牌从弃牌堆飞到抽牌堆的时长（秒）")] [SerializeField]
        private float _reshuffleFlyDuration = 0.2f;

        [Tooltip("洗回飞行牌的起始缩放")] [SerializeField]
        private float _reshuffleFlyScale = 0.2f;

        [Tooltip("洗回起落点的散开量（画布像素）：x = 横向按序号对称铺开的单张间距，y = 纵向奇偶反向的散开量（取 x 的一半即旧的等比观感）")] [SerializeField]
        private Vector2 _reshuffleFlySpreadPx = new(200f, 100f);

        [Tooltip("洗回牌流两轴的跨度上限（画布像素）：x 与 y 各自压缩对应方向，张数越多每张越挤，牌流不会铺出屏")] [SerializeField]
        private Vector2 _reshuffleFlySpreadMaxPx = new(240f, 120f);

        [Tooltip("弃牌波与洗回波起落点的随机抖动幅度（画布像素）：只打散重叠轨迹、不改朝向")] [SerializeField]
        private float _flyJitterPx = 24f;

        private UI_HandView _handView;
        private Rng _rng;

        // 生命周期：本体由 RequireComponent 保证同节点必有
        private void Awake() => _handView = GetComponent<UI_HandView>();

        // 装配：取域侧事件与随机数；事件清单一处看完，各事件的演出在下面
        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_, Rng rng_)
        {
            _rng = rng_;
            eventDispatcher_.Register<BE_DiscardReshuffled>(OnDiscardReshuffledAsync);
            eventDispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            // 单张弃牌：打出结算后的卡离场（消耗/遗忘时域侧自动跳过）、将来"弃一张"的卡效走离场飞行；
            // 抽牌溢出（手牌已满）的牌从未进过手牌、没有视图，改播"从抽牌堆飞进弃牌堆"的单张小飞行
            eventDispatcher_.Register<BE_CardDiscarded>(OnCardDiscardedAsync);
            eventDispatcher_.Register<BE_CardsDiscarded>(OnCardsDiscardedAsync);
            eventDispatcher_.Register<BE_CardExhausted>(exhausted_ => ExitToPileAsync(exhausted_.Card, _handView.ExhaustPile));
            // 遗忘只借道飞往出牌区、不落堆，故不发进堆通知；永久移除与否由域侧决定，这里只播形式
            eventDispatcher_.Register<BE_CardErased>(erased_ => ExitToPileAsync(erased_.Card, null));
            eventDispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            eventDispatcher_.Register<BE_PlayerTurnEnded>(OnPlayerTurnEndedAsync);
            eventDispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        // 洗回搬运：域侧一次搬完整摞，这里演成整波同飞——洗回多少张就飞多少张，一张飞抵即一张进堆。
        // 借来的包装只取池、不入手牌列与模型索引，牌面沿用上次 Bind 的残留内容（整段飞行都缩得很小，不为它单独刷外观）。
        // 起落点按对称铺开骨架错开保底不重叠，再叠随机抖动打散轨迹；域侧逐事件 await，这段播完才轮到本次抽牌入场
        private async UniTask OnDiscardReshuffledAsync(BE_DiscardReshuffled reshuffled_)
        {
            UI_CardBattle[] cards = new UI_CardBattle[reshuffled_.Count];
            Vector3 from = _handView.DiscardPile.transform.position;
            Vector3 to = _handView.DrawPile.transform.position;

            // 起飞：逐张摆在弃牌堆附近的散开点上，缩到飞行用的起始尺寸
            for (int i = 0; i < reshuffled_.Count; i++)
            {
                UI_CardBattle card = _handView.RentFlyView();
                cards[i] = card;
                card.Place(from + FlyOffset(i, reshuffled_.Count) + Jitter(_handView.DiscardPile.transform),
                    _handView.DiscardPile.transform.rotation);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;
            }

            // 同帧全部起飞，一张落地就通知抽牌堆进了一张（数字随之加并弹一下）
            UniTask[] flights = new UniTask[reshuffled_.Count];
            for (int i = 0; i < reshuffled_.Count; i++)
            {
                UI_CardBattle card = cards[i];
                _handView.DiscardPile.OnCardLeft(); // 起飞即通知弃牌堆"牌离堆"，数字随之减
                flights[i] = FlyToDrawPileAsync(card,
                    to + FlyOffset(i, reshuffled_.Count) + Jitter(_handView.DrawPile.transform));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < reshuffled_.Count; i++) _handView.ReturnFlyView(cards[i]);

            async UniTask FlyToDrawPileAsync(UI_CardBattle card_, Vector3 destination_)
            {
                await FlyOutAsync(card_, destination_, _handView.DrawPile.transform.rotation, _reshuffleFlyDuration);
                _handView.DrawPile.OnCardArrived();
            }
        }

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 槽位与峰值都按现算：手牌超过不缩小的张数上限后每张都有缩放，新牌不能按常态 1 落位，否则比同手牌大一截、能点的范围也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            _handView.DrawPile.OnCardLeft(); // 牌从抽牌堆起飞去手牌：先记离堆，数字在入场飞行之前就减
            CardModel model = drawn_.Card;
            UI_CardBattle view = _handView.Spawn(model);
            view.SetPlayableLook(_handView.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            _handView.TryEvaluateSlot(view, out Vector3 position, out Quaternion rotation, out float slotScale);
            view.Place(_handView.DrawPile.transform.position, _handView.DrawPile.transform.rotation);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次（新牌入场中，重排会跳过它）
            _handView.ApplyLayout();
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * _drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_handView.TryGetView(model, out UI_CardBattle bound) || bound != view)
            {
                _handView.UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(slotScale, _drawSettleDuration);
            _handView.UnmarkEntering(view);
            _handView.ApplyLayout(); // 入场期间行若有过变动（别的牌打出 / 让位），在这里一次性纠正到最新槽位
        }

        // 轮到执行：域侧确认这张已离手（手牌成员关系到此注销），行早在松手那一刻就让过位，这里只等入队补间播完
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_handView.TryGetView(played_.Card, out UI_CardBattle view)) return;
            _handView.RemoveFromHand(view);
            _handView.ApplyLayout();

            await _handView.DropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并整手重排一次——行重新开缝，这张从队列位置飞回自己的槽位
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (!_handView.TryGetView(rejected_.Card, out UI_CardBattle view)) return UniTask.CompletedTask;
            if (!_handView.DropZone.Remove(view)) return UniTask.CompletedTask;
            _handView.ApplyLayout();
            return UniTask.CompletedTask;
        }

        // 玩家回合结束：进入弃牌演出即落闸——手牌下沉置灰、取消瞄准，不再接受拖拽与出牌；
        // 开闸的唯一时机仍是下个玩家回合开始的 BE_TurnStarted，中间整段敌方回合都保持落闸
        private UniTask OnPlayerTurnEndedAsync(BE_PlayerTurnEnded ended_)
        {
            _handView.ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再对账退回仍挂着等结算的牌，
        // 最后把三堆数字对齐域侧活值兜底——万有一条进堆 / 离堆通知没发到，最迟在这一刻纠正
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            _handView.ApplyPlayable(started_.IsPlayerTurn);
            _handView.ReconcilePending();
            _handView.DrawPile.SyncToLive();
            _handView.DiscardPile.SyncToLive();
            _handView.ExhaustPile.SyncToLive();
            return UniTask.CompletedTask;
        }

        // 战斗结束：不再有回合交替，手牌一律收起为不可打
        private UniTask OnBattleEndedAsync(BE_BattleEnded ended_)
        {
            _handView.ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 批量弃牌波：整手未保留的牌同帧各自飞向弃牌堆并缩到 0，全部落地才算波次播完（其后域侧才推进到状态衰减与敌方回合）。
        // 起飞逐张记离堆（数字一次掉到底），落地逐张记进堆（数字随落地递增）；起落位各带随机抖动打散重叠轨迹
        private async UniTask OnCardsDiscardedAsync(BE_CardsDiscarded discarded_)
        {
            Transform target = _handView.DiscardPile.transform;
            List<UniTask> flights = new();

            foreach (CardModel model in discarded_.Cards)
            {
                if (!_handView.TryGetView(model, out UI_CardBattle view)) continue;
                _handView.RemoveFromHand(view);
                _handView.DropZone.Remove(view);
                view.Place(view.WorldPosition + Jitter(target), view.WorldRotation);
                _handView.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
                _handView.DiscardPile.OnCardLeft();
                flights.Add(FlyToDiscardPileAsync(view, model, target));
            }

            if (flights.Count == 0) return;
            _handView.ApplyLayout();

            await UniTask.WhenAll(flights);

            // 波内单张归宿飞行：飞向牌堆并缩到 0，落定即通知进堆、注销索引并回收进池
            async UniTask FlyToDiscardPileAsync(UI_CardBattle view_, CardModel model_, Transform target_)
            {
                await FlyOutAsync(view_, target_.position + Jitter(target_), target_.rotation, _exitDuration);
                _handView.DiscardPile.OnCardArrived();
                _handView.ReturnView(model_, view_);
            }
        }

        // 归宿：离手 → 出队 → 整体重排 → 飞向目标牌堆（传 null 即飞往出牌区）并缩到 0 → 落地通知目标堆进了一张 → 注销索引、回收进池。
        // 进堆通知只在落地这一帧发出，故牌堆的数字与弹跳与飞行同步；目标为 null（遗忘借道）时没有"进堆"这回事，不发通知
        private async UniTask ExitToPileAsync(CardModel model_, UI_Pile arrival_)
        {
            if (!_handView.TryGetView(model_, out UI_CardBattle view)) return;
            _handView.RemoveFromHand(view);
            _handView.DropZone.Remove(view);
            _handView.ApplyLayout();

            Transform target = arrival_ != null ? arrival_.transform : _handView.DropZone.transform;
            _handView.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
            await FlyOutAsync(view, target.position, target.rotation, _exitDuration);

            if (arrival_ != null) arrival_.OnCardArrived();
            _handView.ReturnView(model_, view);
        }

        // 单张弃牌的来路分流：手牌区里还有这张牌的视图 → 走离场飞行（打出结算后的离场、将来"弃一张"的卡效）；
        // 查不到视图 → 必是抽牌溢出的那类（手牌已满，牌刚从抽牌堆抽出来就被弃），改演抽牌堆飞进弃牌堆的小飞行
        private async UniTask OnCardDiscardedAsync(BE_CardDiscarded discarded_)
        {
            if (_handView.TryGetView(discarded_.Card, out _))
            {
                await ExitToPileAsync(discarded_.Card, _handView.DiscardPile);
            }
            else
            {
                _handView.DrawPile.OnCardLeft();
                UI_CardBattle card = _handView.RentFlyView();
                card.Place(_handView.DrawPile.transform.position, _handView.DrawPile.transform.rotation);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;

                await FlyOutAsync(card, _handView.DiscardPile.transform.position,
                    _handView.DiscardPile.transform.rotation, _reshuffleFlyDuration);
                _handView.DiscardPile.OnCardArrived();
                _handView.ReturnFlyView(card);
            }
        }

        // 飞行牌的起落点错开量：横向按序号对称铺开、纵向奇偶反向；两轴各自按上限压缩，张数越多每张越挤。
        // 幅度按画布像素给，再按抽牌堆节点的 画布像素→世界 换算成世界向量
        private Vector3 FlyOffset(int index_, int count_)
        {
            Vector2 step = count_ <= 1 ? Vector2.zero : Vector2.Min(_reshuffleFlySpreadPx, _reshuffleFlySpreadMaxPx / (count_ - 1));
            float lateral = (index_ - (count_ - 1) * 0.5f) * step.x;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * step.y;
            return _handView.DrawPile.transform.TransformVector(new Vector2(lateral, vertical));
        }

        // 起落点的随机抖动：±幅度内均匀分布的画布像素，经节点换算成世界向量，只打散重叠轨迹、不改朝向
        private Vector3 Jitter(Transform node_) =>
            node_.TransformVector(new Vector2(_rng.Next(-2f, 2f), _rng.Next(-2f, 2f)) * _flyJitterPx);

        // 飞到目标姿态并缩到 0：三处归宿飞行（离场、弃牌波、洗回）共用的成品段落；起落点的抖动与进堆通知留在各自调用点
        private UniTask FlyOutAsync(UI_CardBattle view_, Vector3 position_, Quaternion rotation_, float duration_) =>
            UniTask.WhenAll(
                view_.MoveToAsync(position_, rotation_, duration_),
                view_.ScaleToAsync(0f, duration_));
    }
}
