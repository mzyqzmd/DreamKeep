using _DreamKeep.RuntimeData;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>弃手牌动作：回合结束把未带「保留」的手牌全部移入弃牌堆。</summary>
    public class Action_DiscardHand : GameAction
    {
        public Action_DiscardHand(Actor source_, List<Actor> targets_) : base(source_, targets_)
        {
        }

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            CardPile hand = battleState_.Hand;
            // 倒序遍历：正序会因移除后的前移覆写而漏检
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                InBattleCard card = hand.Cards[i];
                if (card.IsRetain) continue;
                hand.Remove(card);
                battleState_.DiscardPile.Add(card);
                events_.Emit(new CardDiscardedEvent(card.InstanceId));
            }
        }
    }
}
