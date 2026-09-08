using _DreamKeep.RuntimeData;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>遗忘动作：把自身这张卡从整局 run 永久移除，不进入任何牌堆。</summary>
    public class Action_Erase : GameAction
    {
        public int CardInstanceId { get; }

        public Action_Erase(Actor source_, List<Actor> targets_, int cardInstanceId_)
            : base(source_, targets_) => CardInstanceId = cardInstanceId_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            InBattleCard card = battleState_.Hand.Find(CardInstanceId);
            if (card == null) return;
            battleState_.Hand.Remove(card);
            events_.Emit(new CardErasedEvent(card.InstanceId));
        }
    }
}
