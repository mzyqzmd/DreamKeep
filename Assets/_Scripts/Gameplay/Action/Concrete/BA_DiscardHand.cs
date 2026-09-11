using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>弃手牌动作：回合结束把未带「保留」的手牌全部移入弃牌堆。</summary>
    public class BA_DiscardHand : BattleAction
    {
        public BA_DiscardHand(Actor source_, List<Actor> targets_) : base(source_, targets_) { }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            CardPile hand = battleState_.Hand;
            // 倒序遍历：正序会因移除后的前移覆写而漏检
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                CardModel card = hand.Cards[i];
                if (card.IsRetain) continue;
                hand.Remove(card);
                battleState_.DiscardPile.Add(card);
                events_.Emit(new IBE_CardDiscarded(card));
            }
        }
    }
}
