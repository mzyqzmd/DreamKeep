using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>伤害动作：先扣格挡再扣血；结算时逐目标经数值修正管线（力量/虚弱/易伤）求最终值，
    /// 纯状态结算（灼烧）可显式关闭修正。</summary>
    public sealed class BA_Damage : BattleAction
    {
        /// <summary>是否参与数值修正：灼烧等纯状态结算传 false，不吃力量/虚弱/易伤。</summary>
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
                int amount = _applyModifiers ? DamageHook.Resolve(battleContext_, Source, target, BaseAmount) : BaseAmount;
                int absorbed = Math.Min(target.Block, amount);
                target.Block -= absorbed;
                target.CurrentHp -= amount - absorbed;

                events.Add(new BE_DamageDealt(Source, target, amount, absorbed));
                if (target.IsDead)
                    events.Add(new BE_ActorDied(target, Source));
            }

            return events;
        }
    }
}