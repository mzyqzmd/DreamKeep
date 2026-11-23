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
                    // 恶魔形态：与领地意识同口径，层数即每回合开始获得的力量
                    case BP_DemonForm demonForm:
                        if (demonForm.Layers <= 0) break;
                        BattlePerk demonStrength = PerkBaker.Bake(demonForm.Strength);
                        demonStrength.AddLayers(demonForm.Layers);
                        actions_.Add(new BA_ApplyPerk(host_, new List<Creature> { host_ }, demonStrength));
                        break;
                    // 绯红披风：先付出生命代价再拿格挡，两者分离入队保持先后
                    case BP_CrimsonMantle crimsonMantle:
                        if (crimsonMantle.Layers <= 0) break;
                        actions_.Add(new BA_LoseHealth(host_, new List<Creature> { host_ }, crimsonMantle.HpCost));
                        actions_.Add(new BA_Block(host_, new List<Creature> { host_ }, crimsonMantle.Layers));
                        break;
                    // 狱火：回合开始只负责失血，群伤由 HpDropHook 统一接住
                    case BP_Inferno inferno:
                        actions_.Add(new BA_LoseHealth(host_, new List<Creature> { host_ }, inferno.HpCost));
                        break;
                    // 仪式：与恶魔形态同口径，层数即每回合开始获得的力量，常驻不递减
                    case BP_Ritual ritual:
                        if (ritual.Layers <= 0 || ritual.Strength == null) break;
                        BattlePerk ritualStrength = PerkBaker.Bake(ritual.Strength);
                        ritualStrength.AddLayers(ritual.Layers);
                        actions_.Add(new BA_ApplyPerk(host_, new List<Creature> { host_ }, ritualStrength));
                        break;
                    // 光辉：剩余回合数即层数，每回合开始额外获得 1 能量
                    case BP_Radiance radiance:
                        if (radiance.Layers > 0)
                            actions_.Add(new BA_GainEnergy(null, new List<Creature> { host_ }, 1));
                        break;
                    // 覆甲：回合结束获得等同层数的格挡，随后由基类递减一层
                    case BP_Plating plating:
                        if (plating.Layers > 0)
                            actions_.Add(new BA_Block(null, new List<Creature> { host_ }, plating.Layers));
                        break;
                    // 下回合格挡：触发后整条移除，层数即格挡值
                    case BP_BlockNextTurn blockNextTurn:
                        if (blockNextTurn.Layers > 0)
                            actions_.Add(new BA_Block(null, new List<Creature> { host_ }, blockNextTurn.Layers));
                        break;
                    // 明晰：剩余回合数即层数，每回合开始额外抽 1 张
                    case BP_ClarityPerk clarity:
                        if (clarity.Layers > 0)
                            actions_.Add(new BA_DrawCard(host_, new List<Creature> { host_ }));
                        break;
                }
            }
        }
    }
}
