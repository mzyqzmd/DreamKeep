using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>配置卡烘焙器：把一张卡面配置烘焙成卡组原型（canonical）——一套基础词条加一组升级补丁，
    /// 运行时升级只应用补丁、不再回查配置；配置只在烘焙期被读一次。</summary>
    public static class CardModelBaker
    {
        #region 按配置烘焙一张卡组原型

        public static CardModel Bake(SO_Card config_)
        {
            CM_Static staticData = new(config_.Icon, config_.Type, config_.Rarity, config_.TargetType,
                config_.CanUpdate && config_.Upgrades.Count > 0);
            staticData.Source = config_; // 存档身份：运行时卡只留这一份配置引用，数值仍全在烘焙结果里
            CM_Runtime runtime = new(config_.Name, config_.Cost, true);
            CardModel card = new(staticData, runtime);
            // 原型回填进共享静态配置：任何副本（含已升级卡）都能经 BaseVersion 回看未升级基础形态
            // （原型受"不可运行时改动"保护，纯展示安全）
            staticData.Base = card;

            // 基础词条与它们的源数据平行收集：源数据只用于给"移除词条"补丁对号，不进运行时
            List<CardAffix> baseAffixes = new();
            List<AffixData> baseSources = new();
            for (int i = 0; i < config_.Affixes.Count; i++)
            {
                CardAffix runtimeAffix = ToRuntime(config_.Affixes[i]);
                if (runtimeAffix == null) continue;
                baseAffixes.Add(runtimeAffix);
                baseSources.Add(config_.Affixes[i]);
            }

            List<CM_ValuePatch> valuePatches = new();
            List<CardAffix> addedAffixes = new();
            List<int> removedIndexes = new();
            for (int i = 0; i < config_.Upgrades.Count; i++)
                ResolveUpgrade(config_.Upgrades[i], baseAffixes, baseSources, valuePatches, addedAffixes, removedIndexes);

            runtime.SetBakedContent(baseAffixes, valuePatches, addedAffixes, removedIndexes);
            return card;
        }

        #endregion

        #region 将配置词条整批转换成运行时词条：只读取数值，运行时不持有配置引用

        private static CardAffix ToRuntime(AffixData affix_)
        {
            return affix_ switch
            {
                AD_ApplyPerk applyPerk => ToApplyPerk(applyPerk),
                AD_Block block => new CA_Block(block),
                AD_Damage damage => new CA_Damage(damage),
                AD_DrawCard drawCard => new CA_DrawCard(drawCard),
                AD_Erase erase => new CA_Erase(erase),
                AD_Exhaust exhaust => new CA_Exhaust(exhaust),
                AD_GainEnergy gainEnergy => new CA_GainEnergy(gainEnergy),
                AD_Heal heal => new CA_Heal(heal),
                AD_LostHealth lostHealth => new CA_LostHealth(lostHealth),
                AD_MoveEnemy moveEnemy => new CA_MoveEnemy(moveEnemy),
                AD_Retain retain => new CA_Retain(retain),
                AD_UnableToPlay unableToPlay => new CA_UnableToPlay(unableToPlay),
                _ => null,
            };
        }

        private static CardAffix ToApplyPerk(AD_ApplyPerk applyPerk_)
        {
            if (applyPerk_.Perk == null) return null;
            // 配置里的状态是模板：烘出独立运行时实例后再叠加词条给出的层数
            BattlePerk runtimePerk = PerkBaker.Bake(applyPerk_.Perk);
            if (runtimePerk == null) return null;
            runtimePerk.AddLayers(applyPerk_.Layers);
            return new CA_ApplyPerk(applyPerk_, runtimePerk);
        }
        #endregion

        #region 解析单条升级补丁：改数值与改状态层数落成"词条下标 + 修正"，追加词条烘成运行时词条，移除词条记下标

        private static void ResolveUpgrade(UpgradeData upgrade_, List<CardAffix> baseAffixes_,
            List<AffixData> baseSources_, List<CM_ValuePatch> valuePatches_, List<CardAffix> addedAffixes_,
            List<int> removedIndexes_)
        {
            switch (upgrade_)
            {
                // 改数值：卡级费用固定落在下标 -1，其余通道先找出承载它的那条词条
                case UD_ModifyValue modifyValue:
                    if (modifyValue.Value == EUpgradeChannel.Cost)
                    {
                        valuePatches_.Add(new CM_ValuePatch(EUpgradeChannel.Cost, -1,
                            new(modifyValue.Additive, modifyValue.Multiplier)));
                        break;
                    }

                    int index = IndexOfValueChannel(modifyValue.Value, baseAffixes_);
                    if (index < 0) break; // 这张牌没有承载该通道的词条，补丁不生效
                    valuePatches_.Add(new CM_ValuePatch(modifyValue.Value, index,
                        new(modifyValue.Additive, modifyValue.Multiplier)));
                    break;

                // 改状态层数：按状态种类找出承载它的那条施加效果词条，补丁的通道留空
                case UD_ModifyPerkLayers modifyPerkLayers:
                    int perkIndex = IndexOfPerkAffix(modifyPerkLayers.Perk, baseAffixes_);
                    if (perkIndex < 0) break; // 这张牌没有施加该状态的词条，补丁不生效
                    valuePatches_.Add(new CM_ValuePatch(null, perkIndex,
                        new StatModifier(modifyPerkLayers.Additive, 1f)));
                    break;

                // 追加词条：与配置词条同样烘成运行时形态，升级时再克隆一份进追加集
                case UD_AddAffix addAffix:
                    CardAffix added = ToRuntime(addAffix.Affix);
                    if (added != null) addedAffixes_.Add(added);
                    break;

                // 移除词条：按源数据种类对号取下标，升级时据此跳过该条基础词条
                case UD_RemoveAffix removeAffix:
                    int removed = IndexOfAffix(removeAffix, baseSources_);
                    if (removed >= 0) removedIndexes_.Add(removed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(upgrade_),
                        $"{upgrade_.GetType().Name} 未登记：漏登记会让该升级静默失效");
            }
        }

        // 找承载指定通道的基础词条下标：复用运行时同一张"通道 → 数值容器"映射表
        private static int IndexOfValueChannel(EUpgradeChannel kind_, List<CardAffix> baseAffixes_)
        {
            for (int i = 0; i < baseAffixes_.Count; i++)
                if (CM_ValuePatch.StatOf(kind_, baseAffixes_[i]) != null) return i;

            return -1;
        }

        // 找承载指定状态层数的基础词条下标：层数住在状态原型上，按状态种类对号
        private static int IndexOfPerkAffix(SO_Perk perk_, List<CardAffix> baseAffixes_)
        {
            Type perkType = perk_ == null ? null : PerkBaker.Bake(perk_)?.GetType();
            for (int i = 0; i < baseAffixes_.Count; i++)
                if (baseAffixes_[i] is CA_ApplyPerk applyPerk && applyPerk.Perk.GetType() == perkType) return i;

            return -1;
        }

        // 找要移除的基础词条下标：按源数据种类对号，同种类有多条时命中第一条
        private static int IndexOfAffix(UD_RemoveAffix removeAffix_, List<AffixData> baseSources_)
        {
            Type affixType = removeAffix_.Affix?.GetType();
            if (affixType == null) return -1;

            for (int i = 0; i < baseSources_.Count; i++)
                if (baseSources_[i].GetType() == affixType) return i;

            return -1;
        }

        #endregion
    }
}
