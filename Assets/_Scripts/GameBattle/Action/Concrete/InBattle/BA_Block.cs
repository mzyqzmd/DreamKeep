using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>格挡动作：为目标累加格挡，格挡优先吸收后续伤害。</summary>
    public class BA_Block : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的格挡值。</summary>
        public int Amount { get; set; }

        public BA_Block(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;
                target.Block += Amount;
                events.Add(new BE_BlockGained(target, Amount));
            }
            return events;
        }
    }
}
