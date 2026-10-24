using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的结算入口：按宿主的状态列表顺序（即施加顺序）产出该时机要执行的动作，
    /// 只产出不执行——变更一律交动作系统；动作打给谁由本处分派，调用方不必声明。</summary>
    internal static class SettleHook
    {
        /// <summary>逐条产出触发动作：结算时机不匹配的状态直接跳过。</summary>
        internal static void Settle(Creature host_, EPerkSettleTiming timing_, List<BattleAction> actions_)
        {
            IReadOnlyList<BattlePerk> powers = host_.Perks;
            for (int i = 0; i < powers.Count; i++)
            {
                if (powers[i].SettleTiming != timing_) continue;
                switch (powers[i])
                {
                    // 再生：无来源纯状态结算
                    case BP_Regen regen:
                        if (regen.Layers > 0)
                            actions_.Add(new BA_Heal(null, new List<Creature> { host_ }, regen.Layers));
                        break;
                }
            }
        }
    }
}
