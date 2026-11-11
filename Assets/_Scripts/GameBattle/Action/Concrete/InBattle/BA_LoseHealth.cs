using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>失去生命动作：无视格挡直接扣血，不参与伤害修正；致命时先问拦截者，拦下则血量停在触发值。</summary>
    public sealed class BA_LoseHealth : BattleAction
    {
        /// <summary>失去的生命值。</summary>
        public int Amount { get; }

        public BA_LoseHealth(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) =>
            Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            if (Amount <= 0) return events;

            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;

                int remaining = target.CurrentHp - Amount;
                // 与伤害共用同一条拦截口径：拦下则血量从不归零，后续自然不产生死亡事实
                if (remaining <= 0 && DeathGuardHook.TryPreventDeath(battleContext_, target, Amount, out int hpAfter_))
                    target.CurrentHp = hpAfter_;
                else
                    target.CurrentHp = remaining;

                events.Add(new BE_HealthLost(target, Amount));
                if (!target.IsDead) continue;

                events.Add(new BE_ActorDied(target, Source));
                DeathHook.Cleanup(battleContext_, target, events);
            }

            return events;
        }
    }
}
