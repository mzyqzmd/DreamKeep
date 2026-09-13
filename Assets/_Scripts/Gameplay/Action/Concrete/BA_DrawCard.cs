using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>抽牌动作：从抽牌堆抽一张入手牌，抽牌堆见底时先把弃牌堆洗回。抽多张请入队多个动作，使每张独立走钩子。</summary>
    public class BA_DrawCard : BattleAction
    {
        public BA_DrawCard(Creature source_, List<Creature> targets_) : base(source_, targets_) { }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (state.DrawPile.Count == 0)
            {
                if (state.DiscardPile.Count == 0) return events;
                state.DiscardPile.MoveAllTo(state.DrawPile);
                state.DrawPile.Shuffle(battleContext_.Rng);
            }

            CardModel drawn = state.DrawPile.DrawTop();
            state.Hand.Add(drawn);
            events.Add(new BE_CardDrawn(drawn));
            return events;
        }
    }
}
