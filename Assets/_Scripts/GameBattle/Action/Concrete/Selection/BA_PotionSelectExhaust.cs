using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>灰水选牌：取手牌快照请求玩家多选消耗；手牌为空静默返回空事件，药水已在更早的扣槽动作里消耗。</summary>
    public sealed class BA_PotionSelectExhaust : BattleAction
    {
        public BA_PotionSelectExhaust(Creature source_, List<Creature> targets_)
            : base(source_, targets_)
        {
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<CardModel> candidates = new(battleContext_.Player.CombatState.Hand.Cards);
            if (candidates.Count == 0) return new List<IBattleEvent>();

            return new List<IBattleEvent>
            {
                new BE_CardSelectionRequested(ECardSelectMode.Exhaust, candidates.Count, candidates, Source, Targets),
            };
        }
    }
}
