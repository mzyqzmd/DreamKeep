using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using UnityEngine;

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
                // 药水格挡不参与敏捷修正（敏捷只加"从卡牌中获得"的格挡），故来源传 null
                case SO_BlockPotion block:
                    actions.Add(new BA_Block(null, targets_, block.Amount));
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
                case SO_ExplosiveAmpoule explosive:
                    actions.Add(new BA_Damage(null, targets_, explosive.Damage, false));
                    break;
                case SO_VulnerablePotion vulnerable:
                    AddPerk(actions, source, targets_, vulnerable.Vulnerable, vulnerable.Layers);
                    break;
                case SO_DexterityPotion dexterity:
                    AddPerk(actions, source, targets_, dexterity.Dexterity, dexterity.Layers);
                    break;
                case SO_FyshOil oil:
                    AddPerk(actions, source, targets_, oil.Strength, oil.StrengthLayers);
                    AddPerk(actions, source, targets_, oil.Dexterity, oil.DexterityLayers);
                    break;
                case SO_RegenPotion regen:
                    AddPerk(actions, source, targets_, regen.Regen, regen.Layers);
                    break;
                // 当前格挡的三倍：再获得两倍当前值；读态发生在建队时，先于任何结算动作。
                // 没有格挡时不产生动作，由调用方按"无法结算"拒绝，避免空喝一瓶
                case SO_Fortifier _:
                    if (source.Block <= 0) return actions;
                    actions.Add(new BA_Block(null, targets_, source.Block * 2));
                    break;
                case SO_CureAll cure:
                    actions.Add(new BA_GainEnergy(source, targets_, cure.Energy));
                    for (int i = 0; i < cure.Cards; i++) actions.Add(new BA_DrawCard(source, targets_));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(potion_),
                        $"{potion_.GetType().Name} 未登记：漏登记会让该药水静默失效");
            }

            return actions;
        }

        #endregion

        #region 战斗外饮用：没有战斗态与目标表，只结算不依赖战斗的自身效果

        /// <summary>战斗外结算一瓶药水：效果直接写到玩家身上，不产生事件、不进动作串行。
        /// 只处理自身落点且不依赖战斗态的药水；返回 false 表示这类药水没登记战斗外效果，调用方据此别先扣槽
        /// （可用性与可结算性分处两处，先问再扣才不会出现"药没了、效果也没生效"）。</summary>
        public static bool TryApplyOutsideBattle(SO_Potion potion_, Player player_)
        {
            Creature source = player_.Creature;
            switch (potion_)
            {
                case SO_BloodPotion blood:
                    int amount = (int)(source.MaxHp * blood.HealPercent);
                    source.CurrentHp = Math.Min(source.CurrentHp + amount, source.MaxHp); // 钳制口径与 BA_Heal 一致
                    return true;
                default:
                    Debug.LogError($"{potion_.GetType().Name} 不能在战斗外结算：只登记了自身类且不依赖战斗态的效果");
                    return false;
            }
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