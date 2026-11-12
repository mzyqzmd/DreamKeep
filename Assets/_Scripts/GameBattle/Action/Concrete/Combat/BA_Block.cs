using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>格挡动作：为目标累加格挡（逐目标经 StatHook 求最终值），格挡优先吸收后续伤害。</summary>
    public sealed class BA_Block : BattleAction
    {
        /// <summary>格挡值。</summary>
        public int Amount { get; }

        public BA_Block(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;

                // 修正逐目标求值：同一动作对不同状态的目标结算不同值
                int amount = StatHook.Resolve(battleContext_, EStatChannel.Block, Source, target, Amount);
                target.Block += amount;
                events.Add(new BE_BlockGained(target, amount));
            }
            return events;
        }
    }
}
