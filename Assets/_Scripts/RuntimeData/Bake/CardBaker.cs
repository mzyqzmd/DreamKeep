using System.Collections.Generic;
using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>局外卡烘焙器：把配置卡数据转成一局游戏内的持久卡，配置只在烘焙期被读一次。</summary>
    public static class CardBaker
    {
        #region 按卡 Id 烘焙一张局外卡
        public static InGameCard Bake(string cardId_)
        {
            if (!ConfigLocator.TryGetCard(cardId_, out SO_Card card)) return null;

            InGameCard baked = new(cardId_, card.Cost, card.TargetType, card.CanUpdate && card.UpdatedAffixes.Count > 0);
            FillAffixes(baked, card, false);
            return baked;
        }
        #endregion

        #region 只烘焙词条集合：供敌人意图复用，敌我共用同一条结算管线
        public static List<CardAffix> BakeAffixes(string cardId_)
        {
            List<CardAffix> affixes = new();
            if (!ConfigLocator.TryGetCard(cardId_, out SO_Card card)) return affixes;

            foreach (AffixData affix in card.Affixes)
            {
                CardAffix runtime = ToRuntime(affix);
                if (runtime == null) continue;
                else affixes.Add(runtime);
            }
            return affixes;
        }
        #endregion

        #region 升级重烘焙：清空后按升级态重建整份词条集合，实例 Id 不变
        internal static void Rebake(InGameCard card_)
        {
            if (!ConfigLocator.TryGetCard(card_.CardId, out SO_Card config)) return;
            card_.ClearAffixes();
            FillAffixes(card_, config, card_.IsUpgraded);
        }
        #endregion

        #region 按升级态选择词条集合并有序挂载
        private static void FillAffixes(InGameCard card_, SO_Card config_, bool upgraded_)
        {
            List<AffixData> source = upgraded_ ? config_.UpdatedAffixes : config_.Affixes;
            foreach (AffixData affix in source)
            {
                CardAffix runtime = ToRuntime(affix);
                if (runtime == null) continue;
                else card_.AddAffix(runtime);
            }
        }
        #endregion

        #region 将配置词条转换成运行时词条：只读取数值，运行时不持有配置引用
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
                _ => null,
            };
        }

        private static CardAffix ToApplyPerk(AD_ApplyPerk applyPerk_)
        {
            InBattlePerk perk = PerkBaker.Bake(applyPerk_.Perk, applyPerk_.Layers);
            return perk == null ? null : new CA_ApplyPerk(applyPerk_, perk);
        }

        private static CardAffix ToGrant(AD_Grant grant_)
        {
            CardAffix granted = ToRuntime(grant_.GrantedAffix);
            return granted == null ? null : new CA_Grant(grant_, granted);
        }
        #endregion
    }
}
