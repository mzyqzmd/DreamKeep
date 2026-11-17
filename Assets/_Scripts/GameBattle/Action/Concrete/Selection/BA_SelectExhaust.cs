using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>选择消耗动作：全部档直接消耗整手；恰好档在候选凑不满目标数时整手消耗（没得挑），
    /// 否则请求玩家选满；至多与任意档只要手牌非空就请求选牌（可 0 张）。</summary>
    public sealed class BA_SelectExhaust : BattleAction
    {
        private readonly CA_SelectExhaust _affix;

        public BA_SelectExhaust(Creature source_, List<Creature> targets_, CA_SelectExhaust affix_)
            : base(source_, targets_) => _affix = affix_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<CardModel> candidates = new(battleContext_.Player.CombatState.Hand.Cards);
            if (candidates.Count == 0) return new List<IBattleEvent>();
            if (_affix.Scope == ESelectScope.AllHand ||
                (_affix.Scope == ESelectScope.Exact && candidates.Count <= _affix.Count.Value))
                return new BA_ApplyCardExhaust(Source, Targets, candidates).Execute(battleContext_);

            return new List<IBattleEvent>
            {
                new BE_CardSelectionRequested(ECardSelectMode.Exhaust, _affix.Scope, _affix.Count.Value, candidates,
                    Source, Targets),
            };
        }
    }
}
