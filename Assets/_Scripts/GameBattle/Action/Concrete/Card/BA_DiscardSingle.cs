using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>弃置动作：把指定牌堆中指定的卡移入弃牌堆，卡已离场则静默跳过。</summary>
    public class BA_DiscardSingle : BattleAction
    {
        /// <summary>结算期间卡可能已被消耗或遗忘离场，移除失败即视为离场。</summary>
        public CardModel Card { get; }

        private readonly CardPile _sourcePile;

        public BA_DiscardSingle(Creature source_, List<Creature> targets_, CardModel card_, CardPile sourcePile_)
            : base(source_, targets_)
        {
            Card = card_;
            _sourcePile = sourcePile_;
        }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (!_sourcePile.Remove(Card)) return events;

            state.DiscardPile.Add(Card);
            events.Add(new BE_CardDiscarded(Card));
            return events;
        }
    }
}
