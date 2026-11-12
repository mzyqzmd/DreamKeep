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
                    // 高墙：层数即格挡值，宿主与盟友的落点由动作在执行期解析
                    case BP_Rampart rampart:
                        if (rampart.Layers > 0)
                            actions_.Add(new BA_RampartBlock(host_, rampart.Layers));
                        break;
                    // 领地意识：层数即力量增量，常驻不衰减；走标准施加动作以广播状态施加事件
                    case BP_Territorial territorial:
                        if (territorial.Layers <= 0) break;
                        BattlePerk strength = PerkBaker.Bake(territorial.Strength);
                        strength.AddLayers(territorial.Layers);
                        actions_.Add(new BA_ApplyPerk(host_, new List<Creature> { host_ }, strength));
                        break;
                }
            }
        }
    }
}
