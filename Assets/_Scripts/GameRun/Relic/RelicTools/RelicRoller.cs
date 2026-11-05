using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物掷取服务：多池合并候选（跳过断引用、已持有，可附加过滤）→ 取件交给 Rng。
    /// 整局所有"掷一件/几件未持有遗物"的唯一实现，覆盖遭遇奖励、宝箱、先古三选一、商店货架、
    /// 休息挖掘、事件随机遗物等场景；同引用只收一次，候选全空返回空表。</summary>
    public static class RelicRoller
    {
        #region 收集候选：多池合并、过滤断引用与已持有、同引用去重

        /// <summary>收集多池合并后的可掷候选：断引用与已持有必跳过，extraFilter_ 供商店"跳过已上架"等附加过滤。
        /// 取件由调用方走 Rng：单件 Pick（与局部洗牌取一同消耗一格随机流）、多件 TakeShuffled。</summary>
        public static List<SO_Relic> CollectCandidates(Player player_, IReadOnlyList<SO_RelicPool> pools_,
            Func<SO_Relic, bool> extraFilter_ = null)
        {
            List<SO_Relic> candidates = new();
            HashSet<SO_Relic> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                SO_RelicPool pool = pools_[i];
                if (pool == null) continue;
                IReadOnlyList<SO_Relic> relics = pool.Relics;
                for (int j = 0; j < relics.Count; j++)
                {
                    SO_Relic relic = relics[j];
                    if (relic == null || player_.HasRelic(relic)) continue;
                    if (extraFilter_ != null && !extraFilter_(relic)) continue;
                    if (seen.Add(relic)) candidates.Add(relic);
                }
            }

            return candidates;
        }

        #endregion
    }
}
