using _DreamKeep.RuntimeData;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗动作：把自身这张卡从手牌移入消耗区，本场战斗不再被抽到。</summary>
    public class Action_Exhaust : GameAction
    {
        public int CardInstanceId { get; }

        public Action_Exhaust(Actor source_, List<Actor> targets_, int cardInstanceId_)
            : base(source_, targets_) => CardInstanceId = cardInstanceId_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            InBattleCard card = battleState_.Hand.Find(CardInstanceId);
            if (card == null) return;
            battleState_.Hand.Remove(card);
            battleState_.ExhaustPile.Add(card);
            events_.Emit(new CardExhaustedEvent(card.InstanceId));
        }
    }
}
