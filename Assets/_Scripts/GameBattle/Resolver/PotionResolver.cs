using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>药水转动作工厂：先按落点解析目标，再按具体药水类型产出既有动作，与 CardAffixResolver 同构——
    /// 落点规则只有这一份，视图不重写；无法结算（单体越界、无存活敌人）返回空表，由调用方决定是否拒绝。
    /// 解析出的落点随动作一起交回调用方：表现层据此决定药水飞向谁，不必再算一遍。</summary>
    public static class PotionResolver
    {
        #region 建队：先解析落点，再按药水类型逐个转动作后整体返回（落点经 out 一并交出）

        public static List<BattleAction> Build(BattleContext battleContext_, SO_Potion potion_, Player player_,
            int aimSlot_, out List<Creature> targets_)
        {
            List<BattleAction> actions = new();
            targets_ = ResolveTargets(battleContext_, potion_, player_, aimSlot_);
            if (targets_.Count == 0) return actions;

            Creature source = player_.Creature;
            switch (potion_)
            {
                // 投掷类伤害不参与修正：既不吃力量，也不吃易伤
                case SO_FirePotion fire:
                    actions.Add(new BA_Damage(null, targets_, fire.Amount, false));
                    break;
                case SO_WeakPotion weak:
                    AddPerk(actions, source, targets_, weak.Perk, weak.Layers);
                    break;
                case SO_PotionOfBinding binding:
                    AddPerk(actions, source, targets_, binding.WeakPerk, binding.WeakLayers);
                    AddPerk(actions, source, targets_, binding.VulnerablePerk, binding.VulnerableLayers);
                    break;
                case SO_StrengthPotion strength:
                    AddPerk(actions, source, targets_, strength.Perk, strength.Layers);
                    break;
                case SO_BlockPotion block:
                    actions.Add(new BA_Block(source, targets_, block.Amount));
                    break;
                case SO_EnergyPotion energy:
                    actions.Add(new BA_GainEnergy(source, targets_, energy.Amount));
                    break;
                case SO_SwiftPotion swift:
                    for (int i = 0; i < swift.Count; i++) actions.Add(new BA_DrawCard(source, targets_));
                    break;
                case SO_BloodPotion blood:
                    actions.Add(new BA_Heal(source, targets_, (int)(source.MaxHp * blood.HealPercent)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(potion_),
                        $"{potion_.GetType().Name} 未登记：漏登记会让该药水静默失效");
            }

            return actions;
        }

        #endregion

        // 落点解析：自身 → 玩家；全体 → 存活敌人全表；单体 → 存活敌人里第 aimSlot 个，越界给空表
        private static List<Creature> ResolveTargets(BattleContext battleContext_, SO_Potion potion_, Player player_,
            int aimSlot_)
        {
            if (potion_.TargetType == EPotionTargetType.Self) return new List<Creature> { player_.Creature };

            List<Enemy> alive = battleContext_.GetAliveEnemies();
            if (potion_.TargetType == EPotionTargetType.AllEnemy)
            {
                List<Creature> all = new(alive.Count);
                for (int i = 0; i < alive.Count; i++) all.Add(alive[i].Creature);
                return all;
            }

            if (aimSlot_ < 0 || aimSlot_ >= alive.Count) return new List<Creature>();
            return new List<Creature> { alive[aimSlot_].Creature };
        }

        // 施加状态：定义资产先烘成运行时实例，层数写在实例上；未登记类型烘不出实例即跳过
        private static void AddPerk(List<BattleAction> actions_, Creature source_, List<Creature> targets_,
            SO_Perk data_, int layers_)
        {
            BattlePerk perk = PerkBaker.Bake(data_);
            if (perk == null) return;
            perk.AddLayers(layers_);
            actions_.Add(new BA_ApplyPerk(source_, targets_, perk));
        }
    }
}