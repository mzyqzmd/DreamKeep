using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的受击入口：一次攻击结算后询问一次受击方状态，火焰屏障据此对攻击者反伤；
    /// 反伤走纯状态结算，不吃攻击修正也不会再次触发本入口，避免双方互相反伤递归。</summary>
    internal static class DamageReactHook
    {
        #region 受击后触发：火焰屏障对攻击者反伤

        internal static void Check(BattleContext context_, Creature attacker_, Creature target_, List<IBattleEvent> events_)
        {
            if (target_ == null || target_.IsDead || attacker_ == null || attacker_.IsDead) return;

            BP_FlameBarrier flameBarrier = BattlePerkQuery.Find<BP_FlameBarrier>(target_);
            if (flameBarrier != null && flameBarrier.Layers > 0)
                events_.AddRange(new BA_Damage(target_, new List<Creature> { attacker_ }, flameBarrier.Layers, false, false)
                    .Execute(context_));

            // 荆棘常驻：与火焰屏障同口径反伤，关闭攻击修正与递归触发；两者可叠加各自结算
            BP_Thorns thorns = BattlePerkQuery.Find<BP_Thorns>(target_);
            if (thorns != null && thorns.Layers > 0)
                events_.AddRange(new BA_Damage(target_, new List<Creature> { attacker_ }, thorns.Layers, false, false)
                    .Execute(context_));
        }

        #endregion
    }
}
