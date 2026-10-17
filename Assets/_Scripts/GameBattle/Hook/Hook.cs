using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的唯一入口：值通道求最终值（预览与结算同源）、结算时机收集状态动作；
    /// 分派表只把状态实例路由到它自己的修正方法，通道与交手方判断留在状态类里。</summary>
    internal static class Hook
    {
        #region 求一次数值对指定目标的最终值（预览与结算共用）

        /// <summary>求最终值；来源收集传 null 即全程零分配（只有卡面/飘字要显示"谁改了这个值"时才需要）。</summary>
        internal static int Resolve(BattleContext context_, EStatChannel channel_, Creature source_, Creature target_,
            int baseAmount_, List<BattlePerk> sourceOut_ = null) =>
            Apply(Fold(context_, channel_, source_, target_, sourceOut_), baseAmount_);

        #endregion

        #region 收集宿主在指定结算时机产生的动作（只产出不执行）

        /// <summary>按宿主的状态列表顺序（即施加顺序）逐条产出，变更一律交动作系统执行。</summary>
        internal static void Settle(Creature host_, EPerkSettleTiming timing_, List<Creature> self_,
            List<BattleAction> actions_)
        {
            IReadOnlyList<BattlePerk> powers = host_.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                if (powers[i].SettleTiming != timing_) continue;
                switch (powers[i])
                {
                    // 再生：无来源纯状态结算
                    case BP_Regen regen:
                        if (regen.Layers > 0) actions_.Add(new BA_Heal(null, self_, regen.Layers));
                        break;
                }
            }
        }

        #endregion

        // 分派表：把状态实例路由到它自己的修正方法，未登记的类型不参与数值修正
        private static StatModifier GetModifier(BattlePerk perk_, EStatChannel channel_, Creature host_,
            Creature source_,
            Creature target_) =>
            perk_ switch
            {
                BP_Strength strength => strength.GetModifier(channel_, host_, source_, target_),
                BP_Dexterity dexterity => dexterity.GetModifier(channel_, host_, source_, target_),
                BP_Weak weak => weak.GetModifier(channel_, host_, source_, target_),
                BP_Vulnerable vulnerable => vulnerable.GetModifier(channel_, host_, source_, target_),
                _ => throw new ArgumentOutOfRangeException($"未登记的状态类型：{perk_}")
            };

        // 折叠：遍历玩家与全部敌人身上的状态，死亡宿主不参与
        private static StatModifier Fold(BattleContext context_, EStatChannel channel_, Creature source_,
            Creature target_,
            List<BattlePerk> sourceOut_)
        {
            StatModifier fold = StatModifier.Neutral;
            Collect(context_.Player.Creature, channel_, source_, target_, sourceOut_, ref fold);
            List<Enemy> enemies = context_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                Collect(enemies[i].Creature, channel_, source_, target_, sourceOut_, ref fold);

            return fold;
        }

        // 单个生物的状态贡献：只有贡献非中性的状态才算进了这次修正
        private static void Collect(Creature host_, EStatChannel channel_, Creature source_, Creature target_,
            List<BattlePerk> sourceOut_, ref StatModifier fold_)
        {
            if (host_.IsDead) return;

            IReadOnlyList<BattlePerk> powers = host_.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                StatModifier modifier = GetModifier(powers[i], channel_, host_, source_, target_);
                if (modifier.IsNeutral) continue;

                if (sourceOut_ != null) sourceOut_.Add(powers[i]);
                fold_.additive += modifier.additive;
                fold_.multiplier *= modifier.multiplier;
                if (modifier.floor > fold_.floor) fold_.floor = modifier.floor;
                if (modifier.cap < fold_.cap) fold_.cap = modifier.cap;
            }
        }

        // 套用总修正：先加后乘、clamp 上下限，最后下限归零兜底（物理上不存在负值）
        private static int Apply(StatModifier modifier_, int baseAmount_)
        {
            int value = (int)((baseAmount_ + modifier_.additive) * modifier_.multiplier);
            if (value > modifier_.cap) value = modifier_.cap;
            if (value < modifier_.floor) value = modifier_.floor;
            return value;
        }
    }
}