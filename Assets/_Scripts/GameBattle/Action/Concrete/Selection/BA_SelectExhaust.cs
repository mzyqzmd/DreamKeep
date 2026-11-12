using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>选择消耗动作：全部档直接消耗整手，至多档只要手牌非空就请求玩家选牌（可 0 张）。</summary>
    public sealed class BA_SelectExhaust : BattleAction
    {
        private readonly CA_SelectExhaust _affix;

        public BA_SelectExhaust(Creature source_, List<Creature> targets_, CA_SelectExhaust affix_)
            : base(source_, targets_) => _affix = affix_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<CardModel> candidates = new(battleContext_.Player.CombatState.Hand.Cards);
            if (candidates.Count == 0) return new List<IBattleEvent>();
            if (_affix.Scope == ESelectScope.AllHand)
                return new BA_ApplyCardExhaust(Source, Targets, candidates).Execute(battleContext_);

            return new List<IBattleEvent>
            {
                new BE_CardSelectionRequested(ECardSelectMode.Exhaust, _affix.Count.Value, candidates, Source, Targets),
            };
        }
    }
}
