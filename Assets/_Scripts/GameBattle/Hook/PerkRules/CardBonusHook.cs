using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>按牌加成求值入口：把"当前这张牌"触发的状态加成折进数值基数，与 StatHook 的来源/目标侧属性修正分家；
    /// 结算建队与预览门面共用这一求值，故卡面预览与结算落值在结构上不可能因按牌加成而漂移。
    /// 返回值由调用方加进 baseAmount 后再过 StatHook，分派表只把状态实例路由到它自己的按牌加成方法。</summary>
    internal static class CardBonusHook
    {
        /// <summary>求 source 身上所有状态对指定通道、指定牌的加成合计：零分配，返回值应加进 baseAmount 再由调用方过 StatHook。</summary>
        public static int GetBonus(Creature source_, CardModel card_, EStatChannel channel_)
        {
            if (source_ == null || card_ == null) return 0;

            IReadOnlyList<BattlePerk> perks = source_.Perks;
            int bonus = 0;
            for (int i = 0; i < perks.Count; i++)
                bonus += GetPerkBonus(perks[i], card_, channel_);
            return bonus;
        }

        // 分派表：把状态实例路由到它自己的按牌加成；未登记的类型不参与，通道判断留在这一层
        private static int GetPerkBonus(BattlePerk perk_, CardModel card_, EStatChannel channel_) =>
            perk_ switch
            {
                BP_Fasten fasten when channel_ == EStatChannel.Block => fasten.GetBonus(card_.Name),
                _ => 0
            };
    }
}
