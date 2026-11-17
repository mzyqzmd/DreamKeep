using System.Collections.Generic;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态查询单一入口：按运行时类型在宿主状态表里找实例，供 Hook 与动作判断共用，避免各处重复写遍历循环。</summary>
    public static class BattlePerkQuery
    {
        #region 按类型查找状态实例，找不到返回 null

        public static T Find<T>(Creature host_) where T : BattlePerk
        {
            if (host_ == null) return null;
            IReadOnlyList<BattlePerk> perks = host_.Perks;
            for (int i = 0; i < perks.Count; i++)
                if (perks[i] is T match)
                    return match;

            return null;
        }

        #endregion

        #region 是否挂载某类型状态

        public static bool Has<T>(Creature host_) where T : BattlePerk => Find<T>(host_) != null;

        #endregion

        #region 能量上限加成求和：遍历宿主状态累加，作为上限修正的唯一求和点

        public static int EnergyCapBonus(Creature host_)
        {
            if (host_ == null) return 0;

            int total = 0;
            IReadOnlyList<BattlePerk> perks = host_.Perks;
            for (int i = 0; i < perks.Count; i++)
                total += perks[i].EnergyCapBonus;

            return total;
        }

        #endregion
    }
}