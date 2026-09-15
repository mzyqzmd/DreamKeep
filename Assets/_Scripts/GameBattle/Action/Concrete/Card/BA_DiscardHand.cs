using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>弃手牌动作：回合结束把未带「保留」的手牌全部移入弃牌堆。</summary>
    public class BA_DiscardHand : BattleAction
    {
        public BA_DiscardHand(Creature source_, List<Creature> targets_) : base(source_, targets_) { }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            CardPile hand = state.Hand;
            // 倒序遍历：正序会因移除后的前移覆写而漏检
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                CardModel card = hand.Cards[i];
                if (card.IsRetain) continue;
                hand.Remove(card);
                state.DiscardPile.Add(card);
                events.Add(new BE_CardDiscarded(card));
            }
            return events;
        }
    }
}
