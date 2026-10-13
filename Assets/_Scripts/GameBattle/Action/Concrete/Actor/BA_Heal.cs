using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>治疗动作：为目标恢复血量，用于再生等回复类效果；已死亡目标跳过，与同族动作一致。</summary>
    public sealed class BA_Heal : BattleAction
    {
        /// <summary>回复量。</summary>
        public int Amount { get; }

        public BA_Heal(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;
                target.CurrentHp += Amount;
                events.Add(new BE_HealthRestored(target, Amount));
            }
            return events;
        }
    }
}
