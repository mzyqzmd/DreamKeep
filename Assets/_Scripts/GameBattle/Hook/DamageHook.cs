using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>伤害数值修正管线：对一次伤害逐目标求最终值，预览与结算同源（纯查询、无副作用、不碰随机源）。
    /// 阶段固定顺序：附魔（空槽）→ 加性累加 → 乘性连乘 → 上限折叠（空槽）→ 下限归零；
    /// 修正源按结构序遍历：玩家 → 敌人（列表序），每个生物只看自己挂的状态，适用角色（攻击方/受击方）由行为类判定。</summary>
    public static class DamageHook
    {
        #region 求一次伤害对指定目标的最终值（预览与结算共用）

        public static int Resolve(BattleContext context_, Creature source_, Creature target_, int baseAmount_)
        {
            // 阶段1 附魔（空槽）：来源卡牌自带的修正将来在此先加后乘，位置固定在状态遍历之前

            // 阶段2/3 加性累加 + 乘性连乘：一次遍历同时收集两阶段贡献，数学上等价于"加性整体先于乘性"（乘性作用于加性算完的总和）
            int additive = 0;
            float multiplier = 1f;
            Collect(context_.Player.Creature, source_, target_, ref additive, ref multiplier);
            List<Enemy> enemies = context_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                Collect(enemies[i].Creature, source_, target_, ref additive, ref multiplier);

            int value = (int)((baseAmount_ + additive) * multiplier);

            // 阶段4 上限（空槽）：取所有上限的最小值截断，位置固定在乘性之后

            // 阶段5 下限：任何修正都不把伤害压到负值
            return Math.Max(0, value);
        }

        #endregion

        // 单个生物的状态贡献：加性收进 additive_、乘性收进 multiplier_；死亡生物不再参与
        private static void Collect(Creature host_, Creature source_, Creature target_, ref int additive_, ref float multiplier_)
        {
            if (host_.IsDead) return;

            IReadOnlyList<BattlePerk> powers = host_.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                switch (powers[i])
                {
                    case BP_Strength strength:
                        additive_ += StrengthHook.AdditiveOf(strength, host_, source_);
                        break;
                    case BP_Weak weak:
                        multiplier_ *= WeakHook.MultiplierOf(weak, host_, source_);
                        break;
                    case BP_Vulnerable vulnerable:
                        multiplier_ *= VulnerableHook.MultiplierOf(vulnerable, host_, target_);
                        break;
                }
            }
        }
    }
}
