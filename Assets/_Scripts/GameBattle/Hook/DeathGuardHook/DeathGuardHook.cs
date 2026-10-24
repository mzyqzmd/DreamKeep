using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的死亡入口：致命伤写进生命之前按登记顺序询问拦截者，命中第一个即停——
    /// 域外来源（遗物等）只经 IDeathGuard 参与，战斗域不认识具体实现。</summary>
    internal static class DeathGuardHook
    {
        /// <summary>返回 true 表示已拦下，hpAfter_ 为拦截后的生命值；无人拦截时 hpAfter_ 置零、调用方不读它。</summary>
        internal static bool TryPreventDeath(BattleContext context_, Creature target_, int hpLoss_, out int hpAfter_)
        {
            List<IDeathGuard> guards = context_.DeathGuards;
            for (int i = 0; i < guards.Count; i++)
                if (guards[i].TryPreventDeath(context_, target_, hpLoss_, out hpAfter_)) return true;
            hpAfter_ = 0;
            return false;
        }
    }
}
