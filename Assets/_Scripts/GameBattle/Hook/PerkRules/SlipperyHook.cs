using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>失去生命值统一钳口：Clamp 把单次失去生命钳进 [0, 上限]（有滑溜时上限 1），预览与结算共用；
    /// Consume 在真实掉血后扣一层。与攻击类修正（力量/易伤/虚弱）分家，投掷与遗物伤害不吃攻击修正但照样吃这层防护。</summary>
    internal static class SlipperyHook
    {
        #region 只读钳制：下限 0，有滑溜时上限 1 点

        /// <summary>下限钳到 0（负力量不产生负伤害）；目标仍有滑溜层数时钳到 1；无目标、无状态或耗尽层数时原值返回。</summary>
        internal static int Clamp(Creature target_, int amount_)
        {
            if (amount_ <= 0) return 0;
            if (target_ == null || amount_ == 1) return amount_;
            IReadOnlyList<BattlePerk> perks = target_.Perks;
            for (int i = 0; i < perks.Count; i++)
                if (perks[i] is BP_Slippery slippery && slippery.Layers > 0) return 1;
            return amount_;
        }

        #endregion

        #region 真实掉血后消费一层滑溜

        internal static void Consume(Creature target_, List<IBattleEvent> events_)
        {
            if (target_.IsDead) return;

            IReadOnlyList<BattlePerk> perks = target_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Slippery slippery) continue;
                slippery.AddLayers(-1);
                if (slippery.Layers > 0)
                    events_.Add(new BE_PerkSettled(target_, slippery));
                else
                {
                    BattlePerk removed = target_.RemovePerk(slippery);
                    if (removed != null) events_.Add(new BE_PerkRemoved(target_, removed));
                }
                break;
            }
        }

        #endregion
    }
}
