using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的唯一入口：值通道把状态修正交给 ModifiableStat 求最终值（预览与结算同源）、
    /// 结算时机收集状态动作；分派表只把状态实例路由到它自己的修正方法，通道与交手方判断留在状态类里。</summary>
    internal static class Hook
    {
        // 求值用的临时数值容器：Resolve 同步且不重入，池里通常只有一个实例在轮转
        private static readonly ObjectPool<ModifiableStat> _statPool = new(() => new ModifiableStat(0), 8);

        #region 求一次数值对指定目标的最终值（预览与结算共用）

        /// <summary>求最终值；来源收集传 null 即全程零分配（只有卡面/飘字要显示"谁改了这个值"时才需要）。</summary>
        internal static int Resolve(BattleContext context_, EStatChannel channel_, Creature source_, Creature target_,
            int baseAmount_, List<BattlePerk> sourceOut_ = null)
        {
            ModifiableStat stat = _statPool.Get();
            try
            {
                stat.Reset(baseAmount_);
                Collect(context_.Player.Creature, channel_, source_, target_, sourceOut_, stat);
                List<Enemy> enemies = context_.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                    Collect(enemies[i].Creature, channel_, source_, target_, sourceOut_, stat);
                return stat.Value;
            }
            finally
            {
                _statPool.Release(stat); // 异常路径也归还，池不泄漏实例
            }
        }

        #endregion

        #region 收集宿主在指定结算时机产生的动作（只产出不执行）

        /// <summary>按宿主的状态列表顺序（即施加顺序）逐条产出，变更一律交动作系统执行。</summary>
        internal static void Settle(Creature host_, EPerkSettleTiming timing_, List<Creature> self_,
            List<BattleAction> actions_)
        {
            IReadOnlyList<BattlePerk> powers = host_.Perks;
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

        // 单个生物的状态贡献：只有贡献非中性的状态才算进了这次修正，逐条交给数值容器自行折叠
        private static void Collect(Creature host_, EStatChannel channel_, Creature source_, Creature target_,
            List<BattlePerk> sourceOut_, ModifiableStat stat_)
        {
            if (host_.IsDead) return;

            IReadOnlyList<BattlePerk> powers = host_.Perks;
            for (int i = 0; i < powers.Count; i++)
            {
                StatModifier modifier = GetModifier(powers[i], channel_, host_, source_, target_);
                if (modifier.IsNeutral) continue;

                if (sourceOut_ != null) sourceOut_.Add(powers[i]);
                stat_.Add(modifier);
            }
        }

        // 分派表：把状态实例路由到它自己的修正方法，未登记的类型不参与数值修正
        private static StatModifier GetModifier(BattlePerk perk_, EStatChannel channel_, Creature host_,
            Creature source_, Creature target_) =>
            perk_ switch
            {
                BP_Strength strength => strength.GetModifier(channel_, host_, source_, target_),
                BP_Dexterity dexterity => dexterity.GetModifier(channel_, host_, source_, target_),
                BP_Weak weak => weak.GetModifier(channel_, host_, source_, target_),
                BP_Vulnerable vulnerable => vulnerable.GetModifier(channel_, host_, source_, target_),
                _ => StatModifier.Neutral
            };
    }
}