using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>消耗动作：把打出堆中这张卡移入消耗堆，本场战斗不再被抽到。</summary>
    public class BA_Exhaust : BattleAction
    {
        /// <summary>结算期间卡可能已离场，移除失败即视为离场。</summary>
        private readonly CardModel _cardToExhaust;

        public BA_Exhaust(Creature source_, List<Creature> targets_, CardModel cardToExhaust_) : base(source_, targets_) => _cardToExhaust = cardToExhaust_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (!state.PlayPile.Remove(_cardToExhaust)) return events;

            state.ExhaustPile.Add(_cardToExhaust);
            events.Add(new BE_CardExhausted(_cardToExhaust));
            return events;
        }
    }
}
