using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌演出播放器：本视图关心的八个领域事件都在这里订阅与响应——抽牌入场、打出等待、弃牌/消耗/遗忘的归宿飞行、被拒退回、回合开始与战斗结束。
    /// 它是本视图里唯一 await 演出播完的角色（域侧逐事件 await，演出播完才继续下一件事），且只消费事件、不判断规则。
    /// 位置与缩放的补间全交卡牌根，它只决定"什么时候让哪张牌去哪、等多久"；
    /// 可打状态切换的编排（切闸门 → 清悬停 → 重排 → 刷整卡外观）也收在这里，因为回合开始与战斗结束是它唯一的触发源。</summary>
    public class HandEventPlayer
    {
        private readonly HandCardRegistry _registry;
        private readonly HandFanLayout _layout;
        private readonly HandInteractionGate _gate;
        private readonly HandPlayFlow _playFlow;
        private readonly HandHoverArbiter _arbiter;
        private readonly Obj_PlayCardZone _dropZone;
        private readonly UI_DrawPile _drawPile;
        private readonly UI_DiscardPile _discardPile;
        private readonly UI_ExhaustPile _exhaustPile;
        private readonly float _drawGrowDuration;
        private readonly float _drawGrowOvershoot;
        private readonly float _drawSettleDuration;
        private readonly float _exitDuration;
        private readonly int _exitOrder;

        public HandEventPlayer(HandCardRegistry registry_, HandFanLayout layout_, HandInteractionGate gate_,
            HandPlayFlow playFlow_, HandHoverArbiter arbiter_, Obj_PlayCardZone dropZone_, UI_DrawPile drawPile_,
            UI_DiscardPile discardPile_, UI_ExhaustPile exhaustPile_, float drawGrowDuration_,
            float drawGrowOvershoot_, float drawSettleDuration_, float exitDuration_, int exitOrder_)
        {
            _registry = registry_;
            _layout = layout_;
            _gate = gate_;
            _playFlow = playFlow_;
            _arbiter = arbiter_;
            _dropZone = dropZone_;
            _drawPile = drawPile_;
            _discardPile = discardPile_;
            _exhaustPile = exhaustPile_;
            _drawGrowDuration = drawGrowDuration_;
            _drawGrowOvershoot = drawGrowOvershoot_;
            _drawSettleDuration = drawSettleDuration_;
            _exitDuration = exitDuration_;
            _exitOrder = exitOrder_;
        }

        // 接线：事件清单集中在这里，各事件的演出就在下面，不用翻第二个文件
        public void Register(BattleEventDispatcher dispatcher_)
        {
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

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            CardModel model = drawn_.Card;
            Obj_Card view = _registry.Spawn(model);
            view.SetPlayableLook(_gate.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            _layout.EvaluateSlot(_registry.Count - 1, _registry.Count, out Vector3 position, out Quaternion rotation,
                out _, out int order);
            view.Place(_drawPile.Anchor, Quaternion.identity, order);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次
            _layout.ApplyLayout(view);
            await UniTask.WhenAll(
                view.ScaleToAsync(_drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, order, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_registry.TryGetView(model, out Obj_Card bound) || bound != view)
            {
                _registry.UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(1f, _drawSettleDuration);
            _registry.UnmarkEntering(view);
        }

        // 轮到执行：卡已离手并停在出牌区，等入队补间播完即可，结算期间不飞不缩
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_registry.TryGetView(played_.Card, out Obj_Card view)) return;
            _gate.RemovePending(view);
            _registry.RemoveFromHand(view);
            _layout.ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；卡已不在索引里说明已另行处置，不重复动它
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_) =>
            _registry.TryGetView(rejected_.Card, out Obj_Card view)
                ? _playFlow.RejectAsync(view)
                : UniTask.CompletedTask;

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再把仍悬着等结算的出牌请求对账退回手牌
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ApplyPlayable(started_.IsPlayerTurn);
            _playFlow.ReconcilePending();
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
            if (!_gate.SetPlayable(playable_)) return;
            if (!playable_) _arbiter.ClearHover();
            _layout.ApplyLayout();
            _registry.RefreshPlayableLook(playable_);
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆并缩到 0 → 注销索引、回收进池
        private async UniTask ExitToPileAsync(CardModel model_, Vector3 destination_)
        {
            if (!_registry.TryGetView(model_, out Obj_Card view)) return;
            _gate.RemovePending(view);
            _registry.RemoveFromHand(view);
            _dropZone.Remove(view);
            _layout.ApplyLayout();

            await UniTask.WhenAll(
                view.MoveToAsync(destination_, Quaternion.identity, _exitOrder, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            _registry.Release(model_, view);
        }
    }
}