using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的数值入口：把各生物状态的值修正折进基础值，预览与结算共用同一份规则，
    /// 于是"卡面预览的伤害"与"实际扣掉的血"在结构上不可能不一致；
    /// 分派表只把状态实例路由到它自己的修正方法，通道与交手方判断留在状态类里。</summary>
    internal static class StatHook
    {
        // 求值用的临时数值容器：Resolve 同步且不重入，池里通常只有一个实例在轮转
        private static readonly ObjectPool<ModifiableStat> _statPool = new(() => new ModifiableStat(0), 8);

        /// <summary>求最终值；来源收集传 null 即全程零分配（只有卡面/飘字要显示"谁改了这个值"时才需要）。</summary>
        internal static int Resolve(BattleContext context_, EStatChannel channel_, Creature source_, Creature target_,
            int baseAmount_, List<BattlePerk> sourceOut_ = null)
        {
            ModifiableStat stat = _statPool.Get();
            try
            {
                stat.Reset(baseAmount_);
                Collect(context_.Player.Creature, channel_, source_, target_, sourceOut_, stat);
                IReadOnlyList<Enemy> enemies = context_.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                    Collect(enemies[i].Creature, channel_, source_, target_, sourceOut_, stat);
                return stat.Value;
            }
            finally
            {
                _statPool.Release(stat); // 异常路径也归还，池不泄漏实例
            }
        }

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
