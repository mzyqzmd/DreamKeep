using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>伤害数值修正管线：对一次伤害逐目标求最终值，预览与结算同源（纯查询、无副作用、不碰随机源）。
    /// 阶段固定顺序：附魔（空槽）→ 加性累加 → 乘性连乘 → 上限折叠（空槽）→ 下限归零；
    /// 修正源按结构序遍历：玩家 → 敌人（列表序），每个生物只看自己挂的状态，适用角色（攻击方/受击方）由状态自己判定；
    /// 状态只要覆写 BattlePerk 的三个修正虚方法就会被本管线认识，此处不列举任何状态类型。</summary>
    public static class DamageHook
    {
        #region 求一次伤害对指定目标的最终值（预览与结算共用）

        /// <summary>求最终伤害；来源收集传 null 即全程零分配（只有卡面/飘字要显示"谁改了这个值"时才需要）。</summary>
        public static int Resolve(BattleContext context_, Creature source_, Creature target_, int baseAmount_,
            List<BattlePerk> sourceOut_ = null)
        {
            // 阶段1 附魔（空槽）：来源卡牌自带的修正将来在此先加后乘，位置固定在状态遍历之前

            // 阶段2/3/4 加性、乘性、上限：同一次遍历收集三者，数学上等价于分阶段各遍历一遍
            int additive = 0;
            float multiplier = 1f;
            int cap = int.MaxValue;
            Collect(context_.Player.Creature, source_, target_, sourceOut_, ref additive, ref multiplier, ref cap);
            List<Enemy> enemies = context_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                Collect(enemies[i].Creature, source_, target_, sourceOut_, ref additive, ref multiplier, ref cap);

            int value = (int)((baseAmount_ + additive) * multiplier);
            if (value > cap) value = cap;

            // 阶段5 下限：任何修正都不把伤害压到负值
            return Math.Max(0, value);
        }

        #endregion

        // 单个生物的状态贡献：加性/乘性/上限三路分别收进累加器，只有贡献非中性的状态才算进了这次修正；死亡生物不再参与
        private static void Collect(Creature host_, Creature source_, Creature target_, List<BattlePerk> sourceOut_,
            ref int additive_, ref float multiplier_, ref int cap_)
        {
            if (host_.IsDead) return;

            IReadOnlyList<BattlePerk> powers = host_.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                BattlePerk perk = powers[i];
                int additive = perk.ModifyDamageAdditive(host_, source_, target_);
                float multiplier = perk.ModifyDamageMultiplicative(host_, source_, target_);
                int cap = perk.CapDamage(host_, source_, target_);

                if (sourceOut_ != null && (additive != 0 || multiplier != 1f || cap != int.MaxValue))
                    sourceOut_.Add(perk);

                additive_ += additive;
                multiplier_ *= multiplier;
                if (cap < cap_) cap_ = cap;
            }
        }
    }
}
