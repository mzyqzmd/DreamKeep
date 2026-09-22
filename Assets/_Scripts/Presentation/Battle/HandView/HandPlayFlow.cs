using System;
using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>打牌与表现：前半是出牌流程——管住"抓起 → 松手 → 落点判定 → 入队等待 → 被拒退回 / 归宿离场"这一条线，出牌区队列由它入队出队；
    /// 状态不在这里（拖拽身份与悬停身份都记在手牌登记处，"已打出、等结算"的成员关系由出牌区队列唯一持有），
    /// 它只按登记处与落点做取舍，判定通过即直接向命令门面发出打出意图——本视图唯一向域侧发命令的角色。
    /// 后半是本视图关心的九个领域事件的订阅与全部演出——抽牌入场、洗回搬运、打出等待、弃牌/消耗/遗忘的归宿飞行、被拒退回、回合开始与战斗结束。
    /// "牌进堆 / 离堆"的计数通知在起飞的与落地的那一帧发出（谁飞谁通知），三个牌堆的显示数与弹跳归它们自己；
    /// 洗回时多少张就飞多少张，逐张涨数与逐张弹跳因此与飞行的节奏同源。
    /// 它是本视图里唯一 await 演出播完的角色（域侧逐事件 await，演出播完才继续下一件事），且只消费事件、不判断规则。
    /// 位置与缩放的补间全交包装（UI_CardHolder），它只决定"什么时候让哪张牌去哪、等多久"；所有落点都换成卡牌坐标框架（手牌中心节点）里的本地坐标，牌堆锚点由手牌空间反变换进来；
    /// 换层也在这里按状态发生：抓起进交互层、入队进队列层、离场/洗回/遗忘起飞进飞行层，回手一律由手牌空间换回手牌层并按手牌序号插回；
    /// 可打状态切换的编排（切登记处可打位 → 清悬停 → 重排 → 刷整卡外观）也收在这里，因为回合开始与战斗结束是它唯一的触发源。
    /// 命令被域侧拒绝时由退回事件走 RejectAsync，回合切换时由 ReconcilePending 把仍挂在出牌区的牌全部对账回手牌。</summary>
    public class HandPlayFlow
    {
        // 目标选择尚未实现，槽位固定打首个敌人，等敌人视图落地后再由落点决定
        private const int DefaultAimSlot = 0;

        private readonly HandCardRegistry _registry;
        private readonly HandSpace _space;
        private readonly UI_PlayCardZone _dropZone;
        private readonly BattleCommandDispatcher _commandDispatcher;
        private readonly UI_DrawPile _drawPile;
        private readonly UI_DiscardPile _discardPile;
        private readonly UI_ExhaustPile _exhaustPile;
        private readonly float _drawGrowDuration;
        private readonly float _drawGrowOvershoot;
        private readonly float _drawSettleDuration;
        private readonly float _exitDuration;
        private readonly float _reshuffleFlyDuration;
        private readonly float _reshuffleFlyScale;
        private readonly float _reshuffleFlyStagger;
        private readonly float _reshuffleFlySpreadPx;
        private readonly float _reshuffleFlySpreadMaxPx;
        private readonly float _reshuffleMaxDuration;

        public HandPlayFlow(HandCardRegistry registry_, HandSpace space_, UI_PlayCardZone dropZone_,
            BattleCommandDispatcher commandDispatcher_, UI_DrawPile drawPile_, UI_DiscardPile discardPile_,
            UI_ExhaustPile exhaustPile_, float drawGrowDuration_, float drawGrowOvershoot_,
            float drawSettleDuration_, float exitDuration_, float reshuffleFlyDuration_,
            float reshuffleFlyScale_, float reshuffleFlyStagger_,
            float reshuffleFlySpreadPx_, float reshuffleFlySpreadMaxPx_,
            float reshuffleMaxDuration_)
        {
            _registry = registry_;
            _space = space_;
            _dropZone = dropZone_;
            _commandDispatcher = commandDispatcher_;
            _drawPile = drawPile_;
            _discardPile = discardPile_;
            _exhaustPile = exhaustPile_;
            _drawGrowDuration = drawGrowDuration_;
            _drawGrowOvershoot = drawGrowOvershoot_;
            _drawSettleDuration = drawSettleDuration_;
            _exitDuration = exitDuration_;
            _reshuffleFlyDuration = reshuffleFlyDuration_;
            _reshuffleFlyScale = reshuffleFlyScale_;
            _reshuffleFlyStagger = reshuffleFlyStagger_;
            _reshuffleFlySpreadPx = reshuffleFlySpreadPx_;
            _reshuffleFlySpreadMaxPx = reshuffleFlySpreadMaxPx_;
            _reshuffleMaxDuration = reshuffleMaxDuration_;
        }

        // 接线：事件清单集中在这里，各事件的演出就在下面，不用翻第二个文件
        public void Register(BattleEventDispatcher dispatcher_)
        {
            dispatcher_.Register<BE_DiscardReshuffled>(OnDiscardReshuffledAsync);
            dispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            dispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            dispatcher_.Register<BE_CardDiscarded>(discarded_ =>
                ExitToPileAsync(discarded_.Card, _discardPile));
            dispatcher_.Register<BE_CardExhausted>(exhausted_ =>
                ExitToPileAsync(exhausted_.Card, _exhaustPile));
            // 遗忘按既定口径飞往打出区域，此处只播表现形式，永久移除与否由域侧决定；只借道不落堆，故不发进堆通知
            dispatcher_.Register<BE_CardErased>(erased_ =>
                ExitToPileAsync(erased_.Card, null));
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            dispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #region 出牌受理：抓起、松手、落点判定与回合对账

        // 抓起：登记拖拽身份、整张牌换到交互层最上（压过悬停那张）并掐掉在飞补间，随后由包装跟手
        public void Grab(UI_CardHolder card_)
        {
            _registry.SetDragged(card_);
            _registry.MoveToFocus(card_);
            card_.KillMotion();
        }

        // 松手：以卡牌在卡牌坐标框架里的锚点判定是否落进出牌区（那块矩形即落点真源），命中即视为选定目标打出，未命中飞回手牌槽位。
        // 松手瞬间本回合已不可打（回合权已交出）时也只回位，不把过期命令塞进队列等下一个玩家回合结算
        public void Release(UI_CardHolder card_)
        {
            _registry.ClearDragged();
            if (!_registry.CardsPlayable || !_dropZone.Accepts(card_.AnchoredPosition))
            {
                _space.ReturnToSlotAsync(card_).Forget();
                return;
            }

            // 入队即挂出：待结算的成员关系由出牌区队列唯一持有，闸门判忙与回合对账都向它查；
            // 同时换到队列层最下——队列层内"队首在最上"，入队晚的排在更前面
            _dropZone.Enqueue(card_);
            _registry.MoveToQueue(card_);
            // 卡已不在手牌索引（已被回收）就静默放弃，不把过期命令发给域侧
            if (_registry.Contains(card_.Card)) _commandDispatcher.PlayCard(card_.Card, DefaultAimSlot);
        }

        // 拖拽被强制中断（包装禁用或回收）时直接落回槽位，不播补间
        public void CancelDrag(UI_CardHolder card_)
        {
            _registry.ClearDragged();
            _space.PlaceToSlot(card_);
        }


        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌；先拷一份队列快照再退，仍在手牌列的才回位，其余交各自归宿流程收尾
        public void ReconcilePending()
        {
            IReadOnlyList<UI_CardHolder> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            UI_CardHolder[] snapshot = new UI_CardHolder[queued.Count];
            for (int i = 0; i < snapshot.Length; i++) snapshot[i] = queued[i];

            foreach (UI_CardHolder card in snapshot)
            {
                _dropZone.Remove(card);
                _space.ReturnToSlotAsync(card).Forget();
            }
        }

        #endregion

        // 洗回搬运：域侧一次搬完整摞，这里把它演成一张一张搬——洗回多少张就飞多少张，一张飞抵即一张进堆；
        // 域侧逐事件 await，这段播完才轮到本次抽牌入场
        private UniTask OnDiscardReshuffledAsync(BE_DiscardReshuffled reshuffled_) => FlyCardsAsync(reshuffled_.Count);

        // 飞行：借同等张数的包装（只取池、不入手牌列与模型索引）从弃牌堆飞到抽牌堆并缩到 0，全部到达才算飞完，飞完随即归还卡池；
        // 起飞那一刻通知弃牌堆"牌离堆"（数字随之减），落地那一刻通知抽牌堆"牌进堆"（数字随之加并弹一下）——
        // 两堆的数字与弹跳都由这两条通知推动，这里只管飞；借来的包装还带着上次 Bind 的残留内容，但整段飞行都缩得很小，不为它单独刷外观
        private async UniTask FlyCardsAsync(int count_)
        {
            int flyCount = count_;
            float stagger = flyCount > 1 ? Mathf.Min(_reshuffleFlyStagger, _reshuffleMaxDuration / flyCount) : 0f;
            UI_CardHolder[] cards = new UI_CardHolder[flyCount];
            Vector2 from = PileAnchor(_discardPile);
            Vector2 to = PileAnchor(_drawPile);

            // 起飞：逐张摆在弃牌堆锚点附近（错开量见 EvaluateFlyOffset），缩到飞行用的起始尺寸
            for (int i = 0; i < flyCount; i++)
            {
                UI_CardHolder card = _registry.RentFlyView();
                cards[i] = card;
                card.Place(from + EvaluateFlyOffset(i, flyCount), Quaternion.identity);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;
            }

            // 逐张错开起飞：起飞间隔按总时长上限自动压缩，张数再多也不久等；全部到达才算飞完
            UniTask[] flights = new UniTask[flyCount];
            for (int i = 0; i < flyCount; i++)
            {
                if (i > 0) await UniTask.Delay(TimeSpan.FromSeconds(stagger), DelayType.DeltaTime);
                UI_CardHolder card = cards[i];
                _discardPile.OnCardLeft();
                flights[i] = FlyOneAsync(card, to + EvaluateFlyOffset(i, flyCount));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < flyCount; i++) _registry.ReturnFlyView(cards[i]);
        }

        // 单张飞行：飞到终点并缩到 0，落定即通知抽牌堆进了一张
        private async UniTask FlyOneAsync(UI_CardHolder card_, Vector2 destination_)
        {
            await UniTask.WhenAll(
                card_.MoveToAsync(destination_, Quaternion.identity, _reshuffleFlyDuration),
                card_.ScaleToAsync(0f, _reshuffleFlyDuration));
            _drawPile.OnCardArrived();
        }

        // 飞行牌的起落点错开量（画布像素）：横向按序号对称铺开、纵向奇偶反向；整串牌的总跨度不超过上限，
        // 张数越多每张越挤，一摞厚弃牌堆也不会把牌流铺到画布之外
        private Vector2 EvaluateFlyOffset(int index_, int count_)
        {
            float spread = count_ <= 1 ? 0f : Mathf.Min(_reshuffleFlySpreadPx, _reshuffleFlySpreadMaxPx / (count_ - 1));
            float lateral = (index_ - (count_ - 1) * 0.5f) * spread;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * spread * 0.5f;
            return new Vector2(lateral, vertical);
        }

        // 牌堆锚点 → 卡牌坐标框架（手牌中心节点）里的本地坐标：牌堆与卡牌同在一张画布下，反变换一次即可
        private Vector2 PileAnchor(Component pile_) => _space.ToContainer(pile_.transform);

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 峰值与落位尺寸都按槽位缩放换算：手牌超过不缩小的张数上限后每张都有缩放，新牌不能按常态 1 落位，否则比同手牌大一截、命中面也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            _drawPile.OnCardLeft(); // 牌从抽牌堆起飞去手牌：先记离堆，数字在入场飞行之前就减
            CardModel model = drawn_.Card;
            UI_CardHolder view = _registry.Spawn(model);
            view.SetPlayableLook(_registry.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            _space.EvaluateSlot(_registry.Count - 1, _registry.Count, out Vector2 position, out Quaternion rotation,
                out float slotScale);
            view.Place(PileAnchor(_drawPile), Quaternion.identity);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次
            _space.ApplyLayout(view);
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * _drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_registry.TryGetView(model, out UI_CardHolder bound) || bound != view)
            {
                _registry.UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(slotScale, _drawSettleDuration);
            _registry.UnmarkEntering(view);
        }

        // 轮到执行：卡已离手并停在出牌区，等入队补间播完即可，结算期间不飞不缩
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_registry.TryGetView(played_.Card, out UI_CardHolder view)) return;
            _registry.RemoveFromHand(view);
            _space.ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；卡已不在索引里说明已另行处置，不重复动它
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (_registry.TryGetView(rejected_.Card, out UI_CardHolder view))
            {
                if (!_dropZone.Remove(view)) return UniTask.CompletedTask;
                return _space.ReturnToSlotAsync(view);
            }

            return UniTask.CompletedTask;
        }

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再把仍挂着等结算的出牌请求对账退回手牌，
        // 最后把三堆数字对齐域侧活值兜底：万有一条进堆 / 离堆通知没发到，最迟在这一刻纠正
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

        // 可打状态切换：先让出悬停身份（不可打后就不可交互，ExitHover 只会清状态、会留下半截放大），
        // 再按槽位真源整体重排（位置、缩放与所在层一起回位），最后统一刷整手牌外观；状态没变则什么都不动
        private void ApplyPlayable(bool playable_)
        {
            if (!_registry.SetPlayable(playable_)) return;
            if (!playable_) _space.ClearHover();
            _space.ApplyLayout();
            _registry.RefreshPlayableLook(playable_);
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆（传 null 即飞往出牌区矩形处）并缩到 0 → 落地通知目标堆进了一张 → 注销索引、回收进池。
        // 进堆通知只在落地这一帧发出，故牌堆的数字与弹跳与飞行同步；目标为 null（遗忘借道）时没有"进堆"这回事，不发通知
        private async UniTask ExitToPileAsync(CardModel model_, UI_Pile arrival_)
        {
            if (!_registry.TryGetView(model_, out UI_CardHolder view)) return;
            _registry.RemoveFromHand(view);
            _dropZone.Remove(view);
            _space.ApplyLayout();

            _registry.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
            Vector2 destination = arrival_ != null ? PileAnchor(arrival_) : _space.ToContainer(_dropZone.transform);
            await UniTask.WhenAll(
                view.MoveToAsync(destination, Quaternion.identity, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            if (arrival_ != null) arrival_.OnCardArrived();
            _registry.Release(model_, view);
        }
    }
}
