using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>弃置动作：把打出堆中指定的卡移入弃牌堆，卡已离场则静默跳过。</summary>
    public class BA_Discard : BattleAction
    {
        /// <summary>结算期间卡可能已被消耗或遗忘离场，移除失败即视为离场。</summary>
        public CardModel Card { get; }

        public BA_Discard(Creature source_, List<Creature> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (!state.PlayPile.Remove(Card)) return events;

            state.DiscardPile.Add(Card);
            events.Add(new BE_CardDiscarded(Card));
            return events;
        }
    }
}
