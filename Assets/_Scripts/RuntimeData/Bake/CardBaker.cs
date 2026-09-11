using System;
using System.Collections.Generic;
using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>配置卡烘焙器：持有卡面配置库，把配置卡数据烘焙成卡组原型，配置只在烘焙期被读一次。</summary>
    public static class CardBaker
    {
        private static SO_CardDatabase _cardDatabase;

        /// <summary>装配期注入配置库，必须在首次烘焙前调用一次。</summary>
        public static void Initialize(SO_CardDatabase database_)
            => _cardDatabase = database_;

        #region 按卡 Id 烘焙一张卡组原型，配置缺失时返回 false 由调用方决定如何处置

        public static bool TryBake(string cardId_, out CardModel card_)
        {
            if (!_cardDatabase.TryGetCard(cardId_, out SO_Card card))
            {
                card_ = null;
                return false;
            }

            card_ = new(cardId_, card.Name, card.Cost, card.CanUpdate && card.UpdatedAffixes.Count > 0);
            FillAffixes(card_, card, false);
            return true;
        }

        #endregion

        #region 升级重烘焙：清空后按升级态重建整份词条集合,保留额外词条

        public static void Rebake(CardModel card_)
        {
            if (!_cardDatabase.TryGetCard(card_.CardId, out SO_Card config)) return;
            card_.ClearAffixes();
            FillAffixes(card_, config, card_.IsUpgraded);
        }

        #endregion

        #region 按卡 Id 解析运行时落点信息：翻译目标类型，配置缺失时返回默认（最前敌人）

        public static CardTargetInfo GetTargetInfo(string cardId_)
        {
            if (!_cardDatabase.TryGetCard(cardId_, out SO_Card config)) return default;
            return CardTargetInfo.From(config.TargetType);
        }

        #endregion

        #region 按升级态选择词条集合并有序挂载：原型卡不可被公开写口子改动，故走免断言通道

        private static void FillAffixes(CardModel card_, SO_Card config_, bool upgraded_)
        {
            List<AffixData> source = upgraded_ ? config_.UpdatedAffixes : config_.Affixes;
            foreach (var affix in source)
            {
                CardAffix runtime = ToRuntime(affix);
                if (runtime == null) continue;
                card_.AddAffixUnchecked(runtime);
            }

            foreach (var affix in card_.AdditionalAffixes)
                card_.AddAffixUnchecked(affix);
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
            BattlePerk perk = PerkBaker.Bake(applyPerk_.Layers, applyPerk_.Perk);
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