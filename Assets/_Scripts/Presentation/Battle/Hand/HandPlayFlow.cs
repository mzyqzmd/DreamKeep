using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌流程与演出：前半管出牌——抓起、松手、落点判定、收进出牌区等待、被拒退回、归宿离场，
    /// 其中 AnyEnemy 卡走瞄准分支（按下即卡定住，拖出箭头指向敌人）；后半订阅十一个领域事件并播全部演出。
    /// 它是手牌区里唯一向域侧发命令、唯一 await 演出播完的角色；瞄准状态（哪张牌、箭头是否已显示、指向哪个槽位）
    /// 只在这里——拖拽与悬停身份在 HandArea，"已打出、等结算"的成员关系在出牌区队列。</summary>
    public class HandPlayFlow
    {
        private readonly HandArea _area;
        private readonly UI_PlayCardZone _dropZone;
        private readonly BattleCommandDispatcher _commandDispatcher;
        private readonly Rng _rng;
        private readonly UI_DrawPile _drawPile;
        private readonly UI_DiscardPile _discardPile;
        private readonly UI_ExhaustPile _exhaustPile;
        private readonly UI_CardArrow _arrow;
        private readonly float _drawGrowDuration;
        private readonly float _drawGrowOvershoot;
        private readonly float _drawSettleDuration;
        private readonly float _exitDuration;
        private readonly float _reshuffleFlyDuration;
        private readonly float _reshuffleFlyScale;
        private readonly float _reshuffleFlySpreadPx;
        private readonly float _reshuffleFlySpreadMaxPx;
        private readonly float _flyJitterPx;

        // 瞄准状态：哪张牌正走"拖箭头指定目标"的模式（AnyEnemy 卡按下即登记），以及箭头是否已越过启动阈值显示出来；
        // 纯流程内部状态——松手分流、强制中断、兜底取消与箭头显隐都读它；"按下走哪种交互"由包装读自己的卡回答，不经此路
        private UI_CardBattle _aimingCard;
        private bool _aimingActive;

        private const int NoAimSlot = -1; // 尚未指向任何敌人：松手时据此取消，不打出

        /// <summary>非瞄准卡的默认目标槽位：按存活列表最前一个结算（前敌、全体、随机等落点解析不读它）。</summary>
        public const int DefaultAimSlot = 0;

        // 当前指向的目标槽位（存活敌人列表下标）：由单位侧的目标提示驱动经宿主每帧回写，打牌命令与箭头命中色共用这一处；
        // AnyEnemy 卡一按下先清成"未指向"，防极快操作下默认值被当成有效目标打出去
        private int _aimSlot = DefaultAimSlot;

        // 装配：演出时长与幅度一律由门面以参数平铺传入，本类不读 Inspector
        public HandPlayFlow(HandArea area_, UI_PlayCardZone dropZone_, BattleCommandDispatcher commandDispatcher_,
            Rng rng_, UI_DrawPile drawPile_, UI_DiscardPile discardPile_, UI_ExhaustPile exhaustPile_,
            UI_CardArrow arrow_, float drawGrowDuration_, float drawGrowOvershoot_, float drawSettleDuration_,
            float exitDuration_, float reshuffleFlyDuration_, float reshuffleFlyScale_, float reshuffleFlySpreadPx_,
            float reshuffleFlySpreadMaxPx_, float flyJitterPx_)
        {
            _area = area_;
            _dropZone = dropZone_;
            _commandDispatcher = commandDispatcher_;
            _rng = rng_;
            _drawPile = drawPile_;
            _discardPile = discardPile_;
            _exhaustPile = exhaustPile_;
            _arrow = arrow_;
            _drawGrowDuration = drawGrowDuration_;
            _drawGrowOvershoot = drawGrowOvershoot_;
            _drawSettleDuration = drawSettleDuration_;
            _exitDuration = exitDuration_;
            _reshuffleFlyDuration = reshuffleFlyDuration_;
            _reshuffleFlyScale = reshuffleFlyScale_;
            _reshuffleFlySpreadPx = reshuffleFlySpreadPx_;
            _reshuffleFlySpreadMaxPx = reshuffleFlySpreadMaxPx_;
            _flyJitterPx = flyJitterPx_;
        }

        // 接线：事件清单一处看完，各事件的演出就在下面
        public void Register(BattleEventDispatcher dispatcher_)
        {
            dispatcher_.Register<BE_DiscardReshuffled>(reshuffled_ => FlyCardsAsync(reshuffled_.Count));
            dispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            dispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            // 单张弃牌：打出结算后的卡离场（消耗/遗忘时域侧自动跳过），将来"弃一张"的卡效也走这里
            dispatcher_.Register<BE_CardDiscarded>(discarded_ => ExitToPileAsync(discarded_.Card, _discardPile));
            dispatcher_.Register<BE_CardsDiscarded>(OnCardsDiscardedAsync);
            dispatcher_.Register<BE_CardExhausted>(exhausted_ => ExitToPileAsync(exhausted_.Card, _exhaustPile));
            // 遗忘只借道飞往出牌区、不落堆，故不发进堆通知；永久移除与否由域侧决定，这里只播形式
            dispatcher_.Register<BE_CardErased>(erased_ => ExitToPileAsync(erased_.Card, null));
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            dispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #region 出牌受理：抓起、松手、落点判定、瞄准与回合对账

        // 抓起：登记拖拽身份，整张牌换到交互层最上（压过悬停那张）并掐掉在飞补间，随后由包装跟手；
        // AnyEnemy 卡在此登记进瞄准模式——松手分流与箭头生命周期以它为准（按下走哪种交互由包装读自己的卡决定，不问这里）；
        // 同时把目标槽位清成"未指向"，防极快操作下默认值被当成有效目标打出去
        public void Grab(UI_CardBattle card_)
        {
            _area.SetDragged(card_);
            _area.MoveToFocus(card_);
            card_.KillMotion();
            _aimingCard = card_.Card.IsAnyEnemyTarget ? card_ : null;
            _aimingActive = false;
            if (_aimingCard != null) _aimSlot = NoAimSlot;
        }

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，首次上报即展开箭头；此后位姿与颜色由 Tick 逐帧刷新
        public void OnAimDragged(UI_CardBattle card_)
        {
            if (card_ != _aimingCard) return;
            _aimingActive = true;
            _arrow.Show(card_.WorldCenter, _area.PointerWorldPosition(), _aimSlot >= 0);
        }

        // 瞄准的逐帧驱动：卡离手或不再被按住时兜底取消；箭头展开期间刷新位姿与颜色
        public void Tick()
        {
            if (_aimingCard == null) return;
            if (_area.Dragged != _aimingCard || !_area.IsInHand(_aimingCard))
            {
                CancelAim();
                return;
            }

            if (_aimingActive) _arrow.Show(_aimingCard.WorldCenter, _area.PointerWorldPosition(), _aimSlot >= 0);
        }

        /// <summary>当前瞄准的目标槽位（存活敌人列表下标，-1 = 未指向敌人）：打牌命令与目标提示预览共用这一处来源，由单位侧每帧回写。</summary>
        public int CurrentAimSlot => _aimSlot;

        /// <summary>回写当前瞄准的目标槽位：-1 = 未指向任何敌人，松手时据此取消。</summary>
        public void SetAimSlot(int slot_) => _aimSlot = slot_;

        // 松手：普通卡以锚点（卡根 pivot＝卡下沿中点）是否落进出牌区矩形判定，瞄准卡以"是否指向了某个敌人"（aimSlot ≥ 0）判定；
        // 通过即入队并发出打牌命令，否则回位。松手瞬间已不可打、或这张牌已在拖拽途中离手（被弃置 / 回收）时也不打出——
        // 不把过期命令塞进队列等下一个玩家回合结算
        public void Release(UI_CardBattle card_)
        {
            bool aiming = card_ == _aimingCard;
            if (aiming)
            {
                if (_aimingActive) _arrow.Hide(); // 箭头显示过才需要收起
                _aimingCard = null;
                _aimingActive = false;
            }

            _area.ClearDragged();
            bool accepted = _area.CardsPlayable && _area.IsInHand(card_) &&
                (aiming ? _aimSlot >= 0 : _dropZone.Accepts(card_.WorldPosition));
            if (!accepted)
            {
                _area.ReturnToSlotAsync(card_).Forget();
                return;
            }

            _dropZone.Enqueue(card_); // 收下即挂出：待结算的成员关系由出牌区队列唯一持有
            _area.ApplyLayout(); // 入队即让位：整手立刻合拢，不等域侧回声
            _commandDispatcher.PlayCard(card_.Card, _aimSlot);
        }

        // 拖拽被强制中断（包装禁用或回收）时直接落回槽位，不播补间；瞄准中的牌走 CancelAim 收箭头
        public void CancelDrag(UI_CardBattle card_)
        {
            if (card_ == _aimingCard)
            {
                CancelAim();
                return;
            }

            _area.ClearDragged();
            _area.PlaceToSlot(card_);
        }

        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌，全部出队后整手重排一次——行重新开缝，退回的牌与其余牌一起归位
        private void ReconcilePending()
        {
            IReadOnlyList<UI_CardBattle> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            List<UI_CardBattle> snapshot = new(queued); // 出队会改动原队列，先拷一份再退
            foreach (UI_CardBattle card in snapshot) _dropZone.Remove(card);
            _area.ApplyLayout();
        }

        #endregion

        // 取消瞄准：收箭头、清身份，把牌瞬移回槽位（中断与兜底路径，不播补间）
        private void CancelAim()
        {
            if (_aimingCard == null) return;
            UI_CardBattle card = _aimingCard;
            _aimingCard = null;
            _aimingActive = false;
            _arrow.Hide();
            _area.ClearDragged();
            _area.PlaceToSlot(card);
        }

        // 洗回搬运：域侧一次搬完整摞，这里演成整波同飞——洗回多少张就飞多少张，一张飞抵即一张进堆。
        // 借来的包装只取池、不入手牌列与模型索引，牌面沿用上次 Bind 的残留内容（整段飞行都缩得很小，不为它单独刷外观）。
        // 起落点按对称铺开骨架错开保底不重叠，再叠随机抖动打散轨迹；域侧逐事件 await，这段播完才轮到本次抽牌入场
        private async UniTask FlyCardsAsync(int count_)
        {
            UI_CardBattle[] cards = new UI_CardBattle[count_];
            Vector3 from = _discardPile.transform.position;
            Vector3 to = _drawPile.transform.position;

            // 起飞：逐张摆在弃牌堆附近的散开点上，缩到飞行用的起始尺寸
            for (int i = 0; i < count_; i++)
            {
                UI_CardBattle card = _area.RentFlyView();
                cards[i] = card;
                card.Place(from + FlyOffset(i, count_) + Jitter(_discardPile.transform),
                    _discardPile.transform.rotation);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;
            }

            // 同帧全部起飞，一张落地就通知抽牌堆进了一张（数字随之加并弹一下）
            UniTask[] flights = new UniTask[count_];
            for (int i = 0; i < count_; i++)
            {
                UI_CardBattle card = cards[i];
                _discardPile.OnCardLeft(); // 起飞即通知弃牌堆"牌离堆"，数字随之减
                flights[i] = FlyToDrawPileAsync(card, to + FlyOffset(i, count_) + Jitter(_drawPile.transform));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < count_; i++) _area.ReturnFlyView(cards[i]);

            async UniTask FlyToDrawPileAsync(UI_CardBattle card_, Vector3 destination_)
            {
                await UniTask.WhenAll(
                    card_.MoveToAsync(destination_, _drawPile.transform.rotation, _reshuffleFlyDuration),
                    card_.ScaleToAsync(0f, _reshuffleFlyDuration));
                _drawPile.OnCardArrived();
            }
        }

        // 飞行牌的起落点错开量：横向按序号对称铺开、纵向奇偶反向；整串牌的总跨度不超过上限，张数越多每张越挤。
        // 幅度按画布像素给，再按抽牌堆节点的 画布像素→世界 换算成世界向量
        private Vector3 FlyOffset(int index_, int count_)
        {
            float spread = count_ <= 1 ? 0f : Mathf.Min(_reshuffleFlySpreadPx, _reshuffleFlySpreadMaxPx / (count_ - 1));
            float lateral = (index_ - (count_ - 1) * 0.5f) * spread;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * spread * 0.5f;
            return _drawPile.transform.TransformVector(new Vector2(lateral, vertical));
        }

        // 起落点的随机抖动：±幅度内均匀分布的画布像素，经节点换算成世界向量，只打散重叠轨迹、不改朝向
        private Vector3 Jitter(Transform node_) =>
            node_.TransformVector(new Vector2(_rng.Next(-2f, 2f), _rng.Next(-2f, 2f)) * _flyJitterPx);

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 槽位与峰值都按手牌区本体现算：手牌超过不缩小的张数上限后每张都有缩放，新牌不能按常态 1 落位，否则比同手牌大一截、能点的范围也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            _drawPile.OnCardLeft(); // 牌从抽牌堆起飞去手牌：先记离堆，数字在入场飞行之前就减
            CardModel model = drawn_.Card;
            UI_CardBattle view = _area.Spawn(model);
            view.SetPlayableLook(_area.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            int count = _area.Count;
            _area.EvaluateSlot(count - 1, count, _area.DodgeOf(count - 1),
                out Vector3 position, out Quaternion rotation, out float slotScale);
            view.Place(_drawPile.transform.position, _drawPile.transform.rotation);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次（新牌入场中，重排会跳过它）
            _area.ApplyLayout();
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * _drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_area.TryGetView(model, out UI_CardBattle bound) || bound != view)
            {
                _area.UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(slotScale, _drawSettleDuration);
            _area.UnmarkEntering(view);
            _area.ApplyLayout(); // 入场期间行若有过变动（别的牌打出 / 让位），在这里一次性纠正到最新槽位
        }

        // 轮到执行：域侧确认这张已离手（手牌成员关系到此注销），行早在松手那一刻就让过位，这里只等入队补间播完
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_area.TryGetView(played_.Card, out UI_CardBattle view)) return;
            _area.RemoveFromHand(view);
            _area.ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并整手重排一次——行重新开缝，这张从队列位置飞回自己的槽位
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (!_area.TryGetView(rejected_.Card, out UI_CardBattle view)) return UniTask.CompletedTask;
            if (!_dropZone.Remove(view)) return UniTask.CompletedTask;
            _area.ApplyLayout();
            return UniTask.CompletedTask;
        }

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再对账退回仍挂着等结算的牌，
        // 最后把三堆数字对齐域侧活值兜底——万有一条进堆 / 离堆通知没发到，最迟在这一刻纠正
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ApplyPlayable(started_.IsPlayerTurn);
            ReconcilePending();
            _drawPile.SyncToLive();
            _discardPile.SyncToLive();
            _exhaustPile.SyncToLive();
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
            if (!_area.SetPlayable(playable_)) return;
            if (!playable_)
            {
                CancelAim();
                _area.ClearHover();
            }

            _area.ApplyLayout();
            _area.RefreshPlayableLook(playable_);
        }

        // 批量弃牌波：整手未保留的牌同帧各自飞向弃牌堆并缩到 0，全部落地才算波次播完（其后域侧才推进到状态衰减与敌方回合）。
        // 起飞逐张记离堆（数字一次掉到底），落地逐张记进堆（数字随落地递增）；起落位各带随机抖动打散重叠轨迹
        private async UniTask OnCardsDiscardedAsync(BE_CardsDiscarded discarded_)
        {
            Transform target = _discardPile.transform;
            List<UniTask> flights = new();

            foreach (CardModel model in discarded_.Cards)
            {
                if (!_area.TryGetView(model, out UI_CardBattle view)) continue;
                _area.RemoveFromHand(view);
                _dropZone.Remove(view);
                view.Place(view.WorldPosition + Jitter(target), view.WorldRotation);
                _area.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
                _discardPile.OnCardLeft();
                flights.Add(FlyToDiscardPileAsync(view, model, target));
            }

            if (flights.Count == 0) return;
            _area.ApplyLayout();

            await UniTask.WhenAll(flights);

            // 波内单张归宿飞行：飞向牌堆并缩到 0，落定即通知进堆、注销索引并回收进池
            async UniTask FlyToDiscardPileAsync(UI_CardBattle view_, CardModel model_, Transform target_)
            {
                await UniTask.WhenAll(
                    view_.MoveToAsync(target_.position + Jitter(target_), target_.rotation, _exitDuration),
                    view_.ScaleToAsync(0f, _exitDuration));
                _discardPile.OnCardArrived();
                _area.Release(model_, view_);
            }
        }

        // 归宿：离手 → 出队 → 整体重排 → 飞向目标牌堆（传 null 即飞往出牌区）并缩到 0 → 落地通知目标堆进了一张 → 注销索引、回收进池。
        // 进堆通知只在落地这一帧发出，故牌堆的数字与弹跳与飞行同步；目标为 null（遗忘借道）时没有"进堆"这回事，不发通知
        private async UniTask ExitToPileAsync(CardModel model_, UI_Pile arrival_)
        {
            if (!_area.TryGetView(model_, out UI_CardBattle view)) return;
            _area.RemoveFromHand(view);
            _dropZone.Remove(view);
            _area.ApplyLayout();

            Transform target = arrival_ != null ? arrival_.transform : _dropZone.transform;
            _area.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
            await UniTask.WhenAll(
                view.MoveToAsync(target.position, target.rotation, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            if (arrival_ != null) arrival_.OnCardArrived();
            _area.Release(model_, view);
        }
    }
}
