using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>伤害动作：先扣格挡再扣血；结算时逐目标经 StatHook 求最终值，
    /// 修正源由 StatHook 分派表向状态取用；纯状态结算由触发侧显式关闭修正。</summary>
    public sealed class BA_Damage : BattleAction
    {
        /// <summary>是否参与数值修正：由触发 resolver 对纯状态结算传 false。</summary>
        private readonly bool _applyModifiers;

        /// <summary>基础伤害值（未修正）。</summary>
        public int BaseAmount { get; }

        public BA_Damage(Creature source_, List<Creature> targets_, int amount_, bool applyModifiers_ = true) : base(source_, targets_)
        {
            BaseAmount = amount_;
            _applyModifiers = applyModifiers_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;

                // 修正逐目标求值：同一张群伤卡对不同易伤状态的目标结算不同值
                int amount = _applyModifiers
                    ? StatHook.Resolve(battleContext_, EStatChannel.Damage, Source, target, BaseAmount)
                    : BaseAmount;
                int absorbed = Math.Min(target.Block, amount);
                target.Block -= absorbed;
                int hpLoss = amount - absorbed;
                int remaining = target.CurrentHp - hpLoss;
                // 致命伤先问拦截者：拦下则血量从不归零，后续自然不产生死亡事实（不播死亡、视图不自毁）
                if (remaining <= 0 && DeathGuardHook.TryPreventDeath(battleContext_, target, hpLoss, out int hpAfter_))
                    target.CurrentHp = hpAfter_;
                else
                    target.CurrentHp = remaining;

                events.Add(new BE_DamageDealt(Source, target, amount, absorbed));
                if (target.IsDead)
                {
                    events.Add(new BE_ActorDied(target, Source));
                    DeathHook.Cleanup(battleContext_, target, events);
                    continue;
                }

                // 实际掉血后询问横冲直撞：命中阈值即在本次结算内嵌套执行打断，事件并入本动作
                if (hpLoss > 0) HpDropHook.Check(battleContext_, target, events);

                // 参与数值修正的伤害才消费滑溜：绕过修正的伤害不吃上限，也不扣层
                if (_applyModifiers && hpLoss > 0) SlipperyHook.Consume(target, events);
            }

            return events;
        }
    }
}