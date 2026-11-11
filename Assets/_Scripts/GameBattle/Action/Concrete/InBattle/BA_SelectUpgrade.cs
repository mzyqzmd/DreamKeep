using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>选择升级动作：只在可升级候选多于目标数时才请求玩家选牌，其余情形直接全量升级。</summary>
    public sealed class BA_SelectUpgrade : BattleAction
    {
        private readonly CA_SelectUpgrade _affix;

        public BA_SelectUpgrade(Creature source_, List<Creature> targets_, CA_SelectUpgrade affix_)
            : base(source_, targets_) => _affix = affix_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<CardModel> candidates = new();
            foreach (CardModel card in battleContext_.Player.CombatState.Hand.Cards)
                if (card.CanUpgrade) candidates.Add(card);

            if (candidates.Count == 0) return new List<IBattleEvent>();
            if (_affix.Scope == ESelectScope.AllHand || candidates.Count <= _affix.Count.Value)
                return new BA_ApplyCardUpgrade(Source, Targets, candidates).Execute(battleContext_);

            return new List<IBattleEvent>
            {
                new BE_CardSelectionRequested(ECardSelectMode.Upgrade, _affix.Count.Value, candidates, Source, Targets),
            };
        }
    }
}
