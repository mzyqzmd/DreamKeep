using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>出牌流程：管住"抓起 → 松手 → 落点判定 → 挂出等待 → 被拒退回 / 归宿离场"这一条线，出牌区队列由它入队出队。
    /// 状态不在这里：拖拽身份与挂出集合都记在交互闸门上，它只按闸门与落点做取舍，判定通过即直接向命令门面发出打出意图。
    /// 命令被域侧拒绝时由退回事件走 RejectAsync，回合切换时由 ReconcilePending 把仍挂着的牌全部对账回手牌。</summary>
    public class HandPlayFlow
    {
        // 目标选择尚未实现，槽位固定打首个敌人，等敌人视图落地后再由落点决定
        private const int DefaultAimSlot = 0;

        private readonly HandCardRegistry _registry;
        private readonly HandInteractionGate _gate;
        private readonly HandFanLayout _layout;
        private readonly Obj_PlayCardZone _dropZone;
        private readonly BattleCommandDispatcher _commandDispatcher;

        public HandPlayFlow(HandCardRegistry registry_, HandInteractionGate gate_, HandFanLayout layout_,
            Obj_PlayCardZone dropZone_, BattleCommandDispatcher commandDispatcher_)
        {
            _registry = registry_;
            _gate = gate_;
            _layout = layout_;
            _dropZone = dropZone_;
            _commandDispatcher = commandDispatcher_;
        }

        // 抓起：登记拖拽身份并掐掉在飞补间，随后由拖拽能力跟手
        public void Grab(Obj_Card card_)
        {
            _gate.SetDragged(card_);
            card_.KillMotion();
        }

        // 松手：以卡牌根所在那一个点判定是否落进出牌区，命中即视为选定目标打出，未命中飞回手牌槽位。
        // 松手瞬间本回合已不可打（回合权已交出）时也只回位，不把过期命令塞进队列等下一个玩家回合结算
        public void Release(Obj_Card card_)
        {
            _gate.ClearDragged();
            if (!_gate.CardsPlayable || !_dropZone.Accepts(card_.transform.position))
            {
                _layout.ReturnToSlotAsync(card_).Forget();
                return;
            }

            _gate.AddPending(card_);
            _dropZone.Enqueue(card_);
            // 卡已不在手牌索引（已被回收）就静默放弃，不把过期命令发给域侧
            if (_registry.Contains(card_.Card)) _commandDispatcher.PlayCard(card_.Card, DefaultAimSlot);
        }

        // 拖拽被强制中断（组件禁用或回收）时直接落回槽位，不播补间
        public void CancelDrag(Obj_Card card_)
        {
            _gate.ClearDragged();
            _layout.PlaceToSlot(card_);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并飞回本来的手牌槽位；未在挂出集合里说明已另行处置，不重复动它
        public UniTask RejectAsync(Obj_Card card_)
        {
            if (!_gate.RemovePending(card_)) return UniTask.CompletedTask;
            _dropZone.Remove(card_);
            return _layout.ReturnToSlotAsync(card_);
        }

        // 回合开始对账：把仍悬在出牌区等结算的牌逐张退回手牌；循环体只动出牌区队列与补间，不改挂出集合，可边遍历边退
        public void ReconcilePending()
        {
            if (_gate.PendingPlays.Count == 0) return;

            foreach (Obj_Card card in _gate.PendingPlays)
            {
                _dropZone.Remove(card);
                _layout.ReturnToSlotAsync(card).Forget();
            }

            _gate.ClearPending();
        }
    }
}
