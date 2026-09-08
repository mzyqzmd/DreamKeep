using _DreamKeep.RuntimeData;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>抽牌动作：从抽牌堆抽一张入手牌，抽牌堆见底时先把弃牌堆洗回。抽多张请入队多个动作，使每张独立走钩子。</summary>
    public class Action_DrawCard : GameAction
    {
        public Action_DrawCard(Actor source_, List<Actor> targets_) : base(source_, targets_)
        {
        }

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            if (battleState_.DrawPile.Count == 0)
            {
                if (battleState_.DiscardPile.Count == 0) return;
                battleState_.DiscardPile.MoveAllTo(battleState_.DrawPile);
                battleState_.DrawPile.Shuffle(battleState_.Rng);
            }

            InBattleCard drawn = battleState_.DrawPile.DrawTop();
            battleState_.Hand.Add(drawn);
            events_.Emit(new CardDrawnEvent(drawn.InstanceId, drawn.CardId));
        }
    }
}
