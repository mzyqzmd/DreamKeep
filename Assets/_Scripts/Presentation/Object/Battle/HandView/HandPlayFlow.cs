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
    /// 它是本视图里唯一 await 演出播完的角色（域侧逐事件 await，演出播完才继续下一件事），且只消费事件、不判断规则。
    /// 位置与缩放的补间全交卡牌根，它只决定"什么时候让哪张牌去哪、等多久"；
    /// 可打状态切换的编排（切登记处可打位 → 清悬停 → 重排 → 刷整卡外观）也收在这里，因为回合开始与战斗结束是它唯一的触发源。
    /// 命令被域侧拒绝时由退回事件走 RejectAsync，回合切换时由 ReconcilePending 把仍挂在出牌区的牌全部对账回手牌。</summary>
    public class HandPlayFlow
    {
        // 目标选择尚未实现，槽位固定打首个敌人，等敌人视图落地后再由落点决定
        private const int DefaultAimSlot = 0;

        private readonly HandCardRegistry _registry;
        private readonly HandSpace _space;
        private readonly Obj_PlayCardZone _dropZone;
        private readonly BattleCommandDispatcher _commandDispatcher;
        private readonly UI_DrawPile _drawPile;
        private readonly UI_DiscardPile _discardPile;
        private readonly UI_ExhaustPile _exhaustPile;
        private readonly float _drawGrowDuration;
        private readonly float _drawGrowOvershoot;
        private readonly float _drawSettleDuration;
        private readonly float _exitDuration;
        private readonly int _exitOrder;
        private readonly float _reshuffleFlyDuration;
        private readonly float _reshuffleFlyScale;
        private readonly int _reshuffleFlyCardCount;
        private readonly float _reshuffleFlyStagger;
        private readonly float _reshuffleFlySpread;
        private readonly int _reshuffleFlyOrder;
        private readonly float _reshuffleRestoreDuration;
        private readonly float _reshuffleStepDuration;
        private readonly float _reshuffleMaxDuration;
        private readonly float _reshufflePunchScale;

        public HandPlayFlow(HandCardRegistry registry_, HandSpace space_, Obj_PlayCardZone dropZone_,
            BattleCommandDispatcher commandDispatcher_, UI_DrawPile drawPile_, UI_DiscardPile discardPile_,
            UI_ExhaustPile exhaustPile_, float drawGrowDuration_, float drawGrowOvershoot_,
            float drawSettleDuration_, float exitDuration_, int exitOrder_, float reshuffleFlyDuration_,
            float reshuffleFlyScale_, int reshuffleFlyCardCount_, float reshuffleFlyStagger_,
            float reshuffleFlySpread_, int reshuffleFlyOrder_, float reshuffleRestoreDuration_,
            float reshuffleStepDuration_, float reshuffleMaxDuration_, float reshufflePunchScale_)
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
            _exitOrder = exitOrder_;
            _reshuffleFlyDuration = reshuffleFlyDuration_;
            _reshuffleFlyScale = reshuffleFlyScale_;
            _reshuffleFlyCardCount = reshuffleFlyCardCount_;
            _reshuffleFlyStagger = reshuffleFlyStagger_;
            _reshuffleFlySpread = reshuffleFlySpread_;
            _reshuffleFlyOrder = reshuffleFlyOrder_;
            _reshuffleRestoreDuration = reshuffleRestoreDuration_;
            _reshuffleStepDuration = reshuffleStepDuration_;
            _reshuffleMaxDuration = reshuffleMaxDuration_;
            _reshufflePunchScale = reshufflePunchScale_;
        }

        // 接线：事件清单集中在这里，各事件的演出就在下面，不用翻第二个文件
        public void Register(BattleEventDispatcher dispatcher_)
        {
            dispatcher_.Register<BE_DiscardReshuffled>(OnDiscardReshuffledAsync);
            dispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            dispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            dispatcher_.Register<BE_CardDiscarded>(discarded_ => ExitToPileAsync(discarded_.Card, _discardPile.Anchor));
            dispatcher_.Register<BE_CardExhausted>(exhausted_ => ExitToPileAsync(exhausted_.Card, _exhaustPile.Anchor));
            // 遗忘按既定口径飞往打出区域，此处只播表现形式，永久移除与否由域侧决定
            dispatcher_.Register<BE_CardErased>(erased_ => ExitToPileAsync(erased_.Card, _dropZone.transform.position));
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            dispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #region 出牌受理：抓起、松手、落点判定与回合对账

        // 抓起：登记拖拽身份并掐掉在飞补间，随后由拖拽能力跟手
        public void Grab(Obj_Card card_)
        {
            _registry.SetDragged(card_);
            card_.KillMotion();
        }

        // 松手：以卡牌根所在那一个点判定是否落进出牌区，命中即视为选定目标打出，未命中飞回手牌槽位。
        // 松手瞬间本回合已不可打（回合权已交出）时也只回位，不把过期命令塞进队列等下一个玩家回合结算
        public void Release(Obj_Card card_)
        {
            _registry.ClearDragged();
            if (!_registry.CardsPlayable || !_dropZone.Accepts(card_.transform.position))
            {
                _space.ReturnToSlotAsync(card_).Forget();
                return;
            }

            // 入队即挂出：待结算的成员关系由出牌区队列唯一持有，闸门判忙与回合对账都向它查
            _dropZone.Enqueue(card_);
            // 卡已不在手牌索引（已被回收）就静默放弃，不把过期命令发给域侧
            if (_registry.Contains(card_.Card)) _commandDispatcher.PlayCard(card_.Card, DefaultAimSlot);
        }

        // 拖拽被强制中断（组件禁用或回收）时直接落回槽位，不播补间
        public void CancelDrag(Obj_Card card_)
        {
            _registry.ClearDragged();
            _space.PlaceToSlot(card_);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；不在出牌区队列里说明已另行处置，不重复动它
        public UniTask RejectAsync(Obj_Card card_)
        {
            if (!_dropZone.Remove(card_)) return UniTask.CompletedTask;
            return _space.ReturnToSlotAsync(card_);
        }

        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌；先拷一份队列快照再退，仍在手牌列的才回位，其余交各自归宿流程收尾
        public void ReconcilePending()
        {
            IReadOnlyList<Obj_Card> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            Obj_Card[] snapshot = new Obj_Card[queued.Count];
            for (int i = 0; i < snapshot.Length; i++) snapshot[i] = queued[i];

            foreach (Obj_Card card in snapshot)
            {
                _dropZone.Remove(card);
                _space.ReturnToSlotAsync(card).Forget();
            }
        }

        #endregion

        // 洗回搬运：借几张卡面从弃牌堆飞到抽牌堆，同时两个数字一张一张搬过去；两段必须同起同落，故统一算好每张间隔后并行 await。
        // 每张间隔按总时长上限自动压缩，张数再多也不久等；域侧逐事件 await，这段播完才轮到本次抽牌入场
        private UniTask OnDiscardReshuffledAsync(BE_DiscardReshuffled reshuffled_)
        {
            int count = reshuffled_.Count;
            float step = count > 0 ? Mathf.Min(_reshuffleStepDuration, _reshuffleMaxDuration / count) : 0f;
            return UniTask.WhenAll(
                FlyCardsAsync(count),
                _discardPile.PlayDrainAsync(count, _reshuffleFlyDuration, step),
                _drawPile.PlayRefillAsync(count, _reshuffleFlyDuration, step, _reshuffleRestoreDuration,
                    _reshufflePunchScale));
        }

        // 飞行：借几张卡面（只取池、不入手牌列与模型索引）从弃牌堆飞到抽牌堆并缩到 0，全部到达才算飞完，飞完随即归还卡池；
        // 借来的卡面还带着上次 Bind 的残留内容，但整段飞行都缩得很小，不为它单独刷外观
        private async UniTask FlyCardsAsync(int count_)
        {
            int flyCount = Mathf.Clamp(count_, 1, _reshuffleFlyCardCount);
            Obj_Card[] cards = new Obj_Card[flyCount];
            Vector3 from = _discardPile.Anchor;
            Vector3 to = _drawPile.Anchor;

            // 起飞：逐张摆在弃牌堆锚点附近（错开量见 EvaluateFlyOffset），缩到飞行用的起始尺寸
            for (int i = 0; i < flyCount; i++)
            {
                Obj_Card card = _registry.RentFlyView();
                cards[i] = card;
                card.Place(from + EvaluateFlyOffset(i, flyCount), Quaternion.identity, _reshuffleFlyOrder);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;
            }

            // 逐张错开起飞，全部到达才算飞完
            UniTask[] flights = new UniTask[flyCount];
            for (int i = 0; i < flyCount; i++)
            {
                if (i > 0) await UniTask.Delay(TimeSpan.FromSeconds(_reshuffleFlyStagger), DelayType.DeltaTime);
                Obj_Card card = cards[i];
                Vector3 destination = to + EvaluateFlyOffset(i, flyCount);
                flights[i] = UniTask.WhenAll(
                    card.MoveToAsync(destination, Quaternion.identity, _reshuffleFlyOrder, _reshuffleFlyDuration),
                    card.ScaleToAsync(0f, _reshuffleFlyDuration));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < flyCount; i++) _registry.ReturnFlyView(cards[i]);
        }

        // 飞行牌的起落点错开量：横向按序号对称铺开、纵向奇偶反向，几张牌错开飞而不是完全重合
        private Vector3 EvaluateFlyOffset(int index_, int count_)
        {
            float lateral = (index_ - (count_ - 1) * 0.5f) * _reshuffleFlySpread;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * _reshuffleFlySpread * 0.5f;
            return new Vector3(lateral, vertical, 0f);
        }

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 峰值与落位尺寸都按槽位缩放换算：手牌超过不缩小的张数上限后每张都有缩放，新牌不能按常态 1 落位，否则比同手牌大一截，碰撞体也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            CardModel model = drawn_.Card;
            Obj_Card view = _registry.Spawn(model);
            view.SetPlayableLook(_registry.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            _space.EvaluateSlot(_registry.Count - 1, _registry.Count, out Vector3 position, out Quaternion rotation,
                out float slotScale, out int order);
            view.Place(_drawPile.Anchor, Quaternion.identity, order);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次
            _space.ApplyLayout(view);
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * _drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, order, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_registry.TryGetView(model, out Obj_Card bound) || bound != view)
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
            if (!_registry.TryGetView(played_.Card, out Obj_Card view)) return;
            _registry.RemoveFromHand(view);
            _space.ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；卡已不在索引里说明已另行处置，不重复动它
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_) =>
            _registry.TryGetView(rejected_.Card, out Obj_Card view)
                ? RejectAsync(view)
                : UniTask.CompletedTask;

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再把仍挂着等结算的出牌请求对账退回手牌
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ApplyPlayable(started_.IsPlayerTurn);
            ReconcilePending();
            return UniTask.CompletedTask;
        }

        // 战斗结束：不再有回合交替，手牌一律收起为不可打
        private UniTask OnBattleEndedAsync(BE_BattleEnded ended_)
        {
            ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 可打状态切换：先让出悬停身份（不可打后就不可交互，ExitHover 只会清状态、会留下半截放大），
        // 再按槽位真源整体重排（位置、缩放与层序一起回位），最后统一刷整手牌外观；状态没变则什么都不动
        private void ApplyPlayable(bool playable_)
        {
            if (!_registry.SetPlayable(playable_)) return;
            if (!playable_) _space.ClearHover();
            _space.ApplyLayout();
            _registry.RefreshPlayableLook(playable_);
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆并缩到 0 → 注销索引、回收进池
        private async UniTask ExitToPileAsync(CardModel model_, Vector3 destination_)
        {
            if (!_registry.TryGetView(model_, out Obj_Card view)) return;
            _registry.RemoveFromHand(view);
            _dropZone.Remove(view);
            _space.ApplyLayout();

            await UniTask.WhenAll(
                view.MoveToAsync(destination_, Quaternion.identity, _exitOrder, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            _registry.Release(model_, view);
        }
    }
}
