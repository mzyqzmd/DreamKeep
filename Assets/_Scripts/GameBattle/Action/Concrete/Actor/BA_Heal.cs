using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>治疗动作：为目标恢复血量，用于再生等回复类效果。</summary>
    public class BA_Heal : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的回复量。</summary>
        public int Amount { get; set; }

        public BA_Heal(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                target.CurrentHp += Amount;
                events.Add(new BE_HealthRestored(target, Amount));
            }
            return events;
        }
    }
}
