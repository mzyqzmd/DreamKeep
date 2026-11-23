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

        /// <summary>是否触发受击反伤：反伤自身传 false，避免双方互相反伤递归。</summary>
        private readonly bool _triggerReact;

        /// <summary>基础伤害值（未修正）。</summary>
        public int BaseAmount { get; }

        public BA_Damage(Creature source_, List<Creature> targets_, int amount_, bool applyModifiers_ = true,
            bool triggerReact_ = true) : base(source_, targets_)
        {
            BaseAmount = amount_;
            _applyModifiers = applyModifiers_;
            _triggerReact = triggerReact_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;

                // 攻击类修正逐目标求值，再由防护类滑溜钳上限；投掷伤害不吃攻击修正但照样受滑溜约束
                int amount = StatResolver.ResolveDamage(battleContext_, EStatChannel.Damage, Source, target,
                    BaseAmount, _applyModifiers);
                int blockAbsorbed = Math.Min(target.Block, amount);
                target.Block -= blockAbsorbed;
                int hpLoss = amount - blockAbsorbed;
                // 缓冲只抵消格挡之后仍未挡住的伤害；抵消量并入 Absorbed，表现层按真实掉血渲染
                int bufferAbsorbed = BufferHook.TryAbsorb(target, hpLoss, events);
                hpLoss -= bufferAbsorbed;
                int remaining = target.CurrentHp - hpLoss;
                // 致命伤先问拦截者：拦下则血量从不归零，后续自然不产生死亡事实（不播死亡、视图不自毁）
                if (remaining <= 0 && DeathGuardHook.TryPreventDeath(battleContext_, target, hpLoss, out int hpAfter_))
                    target.CurrentHp = hpAfter_;
                else
                    target.CurrentHp = remaining;

                events.Add(new BE_DamageDealt(Source, target, amount, blockAbsorbed + bufferAbsorbed));
                if (target.IsDead)
                {
                    events.Add(new BE_ActorDied(target, Source));
                    DeathHook.Cleanup(battleContext_, target, events);
                    continue;
                }

                // 实际掉血后询问横冲直撞：命中阈值即在本次结算内嵌套执行打断，事件并入本动作
                if (hpLoss > 0) HpDropHook.Check(battleContext_, target, events);

                // 攻击被完全格挡后询问失衡：只看真实格挡量，缓冲抵消不该被当成完全格挡
                if (_applyModifiers && amount > 0 && blockAbsorbed == amount)
                    FullBlockHook.Check(battleContext_, Source, events);

                // 真实掉血才消费滑溜；缓冲已在格挡后按未挡伤害自行消费
                if (hpLoss > 0) SlipperyHook.Consume(target, events);

                // 攻击落点已确定后询问受击反伤：被完全格挡也算法受到一次攻击
                if (_applyModifiers && _triggerReact && amount > 0)
                    DamageReactHook.Check(battleContext_, Source, target, events);
            }

            return events;
        }
    }
}