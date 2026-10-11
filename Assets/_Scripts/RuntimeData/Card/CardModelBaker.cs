using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>配置卡烘焙器：把一张卡面配置烘焙成卡组原型（canonical），未升级与已升级两套词条一次烘好，
    /// 运行时升级只切档、不再回查配置；配置只在烘焙期被读一次。</summary>
    public static class CardModelBaker
    {
        #region 按配置烘焙一张卡组原型

        public static CardModel Bake(SO_Card config_)
        {
            CM_Static staticData = new(config_.name, config_.Icon, config_.Type,
                config_.Rarity, config_.TargetType, config_.CanUpdate && config_.UpdatedAffixes.Count > 0);
            CM_Runtime runtime = new(config_.Name, config_.Cost, true);
            CardModel card = new(staticData, runtime);
            runtime.SetBakedAffixes(BuildAffixes(config_.Affixes), BuildAffixes(config_.UpdatedAffixes));
            return card;
        }

        #endregion

        #region 将配置词条整批转换成运行时词条：只读取数值，运行时不持有配置引用

        private static List<CardAffix> BuildAffixes(List<AffixData> source_)
        {
            List<CardAffix> affixes = new();
            for (int i = 0; i < source_.Count; i++)
            {
                CardAffix runtime = ToRuntime(source_[i]);
                if (runtime == null) continue;
                affixes.Add(runtime);
            }

            return affixes;
        }

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
                AD_Grant grant => ToGrant(grant),
                AD_Retain retain => new CA_Retain(retain),
                AD_UnableToPlay unableToPlay => new CA_UnableToPlay(unableToPlay),
                _ => null,
            };
        }

        private static CardAffix ToApplyPerk(AD_ApplyPerk applyPerk_)
        {
            if (applyPerk_.Perk == null) return null;
            // 配置里的状态是模板：克隆出独立实例后再叠加词条给出的层数
            BattlePerk runtimePerk = applyPerk_.Perk.Clone();
            runtimePerk.AddLayers(applyPerk_.Layers);
            return new CA_ApplyPerk(applyPerk_, runtimePerk);
        }

        private static CardAffix ToGrant(AD_Grant grant_)
        {
            CardAffix granted = ToRuntime(grant_.GrantedAffix);
            return granted == null ? null : new CA_Grant(grant_, granted);
        }
        #endregion
    }
}