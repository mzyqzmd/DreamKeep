using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物抽取服务：整局抽遗物的唯一出口，覆盖遭遇奖励、宝箱、先古三选一、商店货架、休息挖掘、事件遗物等。
    /// 常规池候选跳过断引用与已持有（可附加过滤）后按池权重取件、池内等权；池空或件数不足时降级到整局兜底池，补齐到目标件数。
    /// 兜底池随整局状态持有（RunState.FallbackRelicPool，组合根构建时传入），其成员豁免"跳过已持有"与附加过滤，故可重复发放；
    /// 补齐只做尾部追加，不额外消耗随机流，同种子可复现不变。</summary>
    public static class RelicRoller
    {
        #region 抽取：收集候选、按池权重取件、缺口由兜底池补齐

        /// <summary>掷 count_ 件遗物：常规池不足时由兜底池补齐到足额，返回恒定件数（兜底未接线时按实数）。</summary>
        public static List<SO_Relic> Roll(RunState run_, IReadOnlyList<SO_RelicPool> pools_, int count_, Rng rng_,
            Func<SO_Relic, bool> extraFilter_ = null)
        {
            List<SO_Relic> taken = rng_.TakeWeighted(CollectCandidates(run_.Player, pools_, extraFilter_), count_);
            FillFallback(run_.FallbackRelicPool, taken, count_);
            return taken;
        }

        /// <summary>掷一件遗物：与 Roll(..., 1) 同口径，兜底未接线且常规池全空时返回 null。</summary>
        public static SO_Relic RollOne(RunState run_, IReadOnlyList<SO_RelicPool> pools_, Rng rng_,
            Func<SO_Relic, bool> extraFilter_ = null)
        {
            List<SO_Relic> taken = Roll(run_, pools_, 1, rng_, extraFilter_);
            return taken.Count == 0 ? null : taken[0];
        }

        /// <summary>只收集候选、不取件：供"先列候选、点击时才掷"的入口（休息挖掘）使用；候选全空时补兜底保底。
        /// 不消耗随机流，取件时机仍由调用方掌握。</summary>
        public static List<SO_Relic> Collect(RunState run_, IReadOnlyList<SO_RelicPool> pools_,
            Func<SO_Relic, bool> extraFilter_ = null)
        {
            List<SO_Relic> candidates = ProjectItems(CollectCandidates(run_.Player, pools_, extraFilter_));
            if (candidates.Count != 0) return candidates;
            SO_Relic fallback = FirstFallbackRelic(run_.FallbackRelicPool);
            if (fallback != null) candidates.Add(fallback);
            return candidates;
        }

        #endregion

        // 兜底池首件有效遗物：池内按共识只放头环一件，取首件即可保证"全部提供头环"
        private static SO_Relic FirstFallbackRelic(SO_RelicPool fallbackPool_)
        {
            if (fallbackPool_ == null) return null;
            IReadOnlyList<SO_Relic> relics = fallbackPool_.Relics;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null) return relics[i];
            return null;
        }

        // 常规池候选：断引用、已持有、附加过滤一律跳过；同引用只收一次；
        // 每件权重 = 池权重 / 本池贡献件数，单次抽取等价"按池权重轮盘选池、池内等权"
        private static List<WeightedItem<SO_Relic>> CollectCandidates(Player player_, IReadOnlyList<SO_RelicPool> pools_,
            Func<SO_Relic, bool> extraFilter_)
        {
            List<WeightedItem<SO_Relic>> candidates = new();
            if (pools_ == null) return candidates;
            HashSet<SO_Relic> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                SO_RelicPool pool = pools_[i];
                if (pool == null) continue;
                IReadOnlyList<SO_Relic> relics = pool.Relics;
                List<SO_Relic> fresh = null;
                for (int j = 0; j < relics.Count; j++)
                {
                    SO_Relic relic = relics[j];
                    if (relic == null || player_.HasRelic(relic)) continue;
                    if (extraFilter_ != null && !extraFilter_(relic)) continue;
                    if (seen.Add(relic)) (fresh ??= new()).Add(relic);
                }

                if (fresh == null) continue;
                float perItem = pool.Weight / fresh.Count;
                for (int j = 0; j < fresh.Count; j++) candidates.Add(new WeightedItem<SO_Relic>(fresh[j], perItem));
            }

            return candidates;
        }

        // 带权候选投影成裸列表：挖掘自选入口不掷骰，权重无意义
        private static List<SO_Relic> ProjectItems(List<WeightedItem<SO_Relic>> weighted_)
        {
            List<SO_Relic> items = new(weighted_.Count);
            for (int i = 0; i < weighted_.Count; i++) items.Add(weighted_[i].Item);
            return items;
        }

        // 缺口补齐：尾部追加兜底遗物，不洗牌、不消耗随机流
        private static void FillFallback(SO_RelicPool fallbackPool_, List<SO_Relic> result_, int count_)
        {
            SO_Relic fallback = FirstFallbackRelic(fallbackPool_);
            if (fallback == null) return;
            while (result_.Count < count_) result_.Add(fallback);
        }
    }
}
