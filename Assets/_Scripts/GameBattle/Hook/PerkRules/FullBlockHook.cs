using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的全格挡入口：攻击方的攻击被目标完全格挡（吸收等于伤害且伤害为正）时询问一次，
    /// 攻击方带「失衡」即在本次结算内嵌套执行击晕动作（只合事件不进队列，照 HpDropHook 先例）。</summary>
    internal static class FullBlockHook
    {
        #region 全格挡后检查失衡：命中即自击晕，产出的事件并入调用方的收集表

        /// <summary>攻击方带失衡时触发；同类型状态唯一，命中即止。</summary>
        internal static void Check(BattleContext context_, Creature attacker_, List<IBattleEvent> events_)
        {
            if (attacker_.IsDead) return;

            IReadOnlyList<BattlePerk> perks = attacker_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Imbalanced) continue;

                BA_ImbalancedStun stunAction = new(attacker_);
                events_.AddRange(stunAction.Execute(context_));
                break;
            }
        }

        #endregion
    }
}