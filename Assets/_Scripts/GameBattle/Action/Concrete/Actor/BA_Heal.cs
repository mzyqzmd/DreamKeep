using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>治疗动作：为目标恢复血量并钳到上限，用于再生等回复类效果；已死亡目标跳过，与同族动作一致。</summary>
    public sealed class BA_Heal : BattleAction
    {
        /// <summary>请求回复量（实际回复量由事件给出，满血时为 0）。</summary>
        public int Amount { get; }

        public BA_Heal(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;
                int healed = Math.Min(target.CurrentHp + Amount, target.MaxHp) - target.CurrentHp; // 已满血时不涨
                target.CurrentHp += healed;
                events.Add(new BE_HealthRestored(target, healed));
            }
            return events;
        }
    }
}
