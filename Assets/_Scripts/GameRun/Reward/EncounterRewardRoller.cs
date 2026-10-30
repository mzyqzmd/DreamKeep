using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;

namespace _DreamKeep.GameRun
{
    /// <summary>遭遇奖励掷骰：把奖励配置 + 整局状态掷成一份奖励清单，是"配置 → 面板条目"的唯一入口。
    /// 掷骰顺序固定（金币 → 卡牌各组 → 药水 → 遗物）且全部走同一条约会随机流，同种子可复现；
    /// 三类奖励都支持配置多份池，掷骰前先把池列表合并成一份候选集（同引用去重）再无放回抽取；
    /// 配置为空（Boss）或候选集为空时静默跳过该条，池里断引用的项也直接跳过——绝不让结算抛异常卡住流程。</summary>
    public static class EncounterRewardRoller
    {
        /// <summary>每组卡牌奖励的候选张数：恒为三选一，不随配置变化。</summary>
        private const int CardOptionCount = 3;

        #region 掷出一份奖励清单：配置为空时返回空表

        public static List<RunReward> Roll(SO_EncounterReward config_, RunState run_)
        {
            List<RunReward> rewards = new();
            if (config_ == null || run_ == null) return rewards;
            Rng rng = run_.Rng;

            RollGold(config_, rng, rewards);
            RollCards(config_, rng, rewards);
            RollPotion(config_, rng, rewards);
            RollRelics(config_, run_, rng, rewards);

            return rewards;
        }

        #endregion

        // 金币：上限为 0 视为不给；上下限相等时取定值，否则闭区间掷一次
        private static void RollGold(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            int min = config_.GoldMin;
            int max = config_.GoldMax;
            if (max <= 0) return;
            if (min < 0) min = 0;
            int gold = max <= min ? min : rng_.Next(min, max + 1);
            if (gold > 0) rewards_.Add(RunReward.GoldOf(gold));
        }

        // 卡牌：每组独立抽三张不重复的候选；合并后的候选集不足三张时有几张给几张
        private static void RollCards(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.CardRewardCount <= 0) return;
            List<SO_Card> cards = Collect(config_.CardPools, static pool_ => pool_.Cards);
            if (cards.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了 {config_.CardRewardCount} 组卡牌奖励，但卡池列表没有有效卡，已跳过。");
                return;
            }

            for (int i = 0; i < config_.CardRewardCount; i++)
            {
                List<CardModel> options = DrawCards(cards, rng_);
                if (options.Count > 0) rewards_.Add(RunReward.CardsOf(options));
            }
        }

        // 合并多份池成一份候选集：断引用与 keep_ 拒绝的项跳过，同引用只收一次——同一项在两份池里不会重复出现
        private static List<T> Collect<T, TPool>(IReadOnlyList<TPool> pools_, Func<TPool, IReadOnlyList<T>> items_,
            Func<T, bool> keep_ = null)
        {
            List<T> collected = new();
            HashSet<T> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                TPool pool = pools_[i];
                if (pool == null) continue;
                IReadOnlyList<T> items = items_(pool);
                for (int j = 0; j < items.Count; j++)
                {
                    T item = items[j];
                    if (item == null) continue;
                    if (keep_ != null && !keep_(item)) continue;
                    if (seen.Add(item)) collected.Add(item);
                }
            }

            return collected;
        }

        // 不放回抽样：候选集洗牌取前几个再投影，保证同一组里不会出现重复项
        private static List<CardModel> DrawCards(List<SO_Card> cards_, Rng rng_)
        {
            List<SO_Card> taken = rng_.TakeShuffled(cards_, CardOptionCount);
            List<CardModel> options = new(taken.Count);
            for (int i = 0; i < taken.Count; i++) options.Add(CardModelBaker.Bake(taken[i]));
            return options;
        }

        // 药水：先掷概率再抽一瓶；合并后的候选集为空视为没有药水奖励
        private static void RollPotion(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.PotionChance <= 0f) return;
            List<SO_Potion> potions = Collect(config_.PotionPools, static pool_ => pool_.Potions);
            if (potions.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了药水概率，但药水池列表没有有效药水，已跳过。");
                return;
            }

            if (rng_.Next(0f, 1f) >= config_.PotionChance) return;
            rewards_.Add(RunReward.PotionOf(potions[rng_.Next(potions.Count)]));
        }

        // 遗物：合并池后跳过已持有与断引用的项，再不放回抽取；不够件数时按实际数量发，并留一条警告
        private static void RollRelics(SO_EncounterReward config_, RunState run_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.RelicCount <= 0) return;
            List<SO_Relic> candidates = Collect(config_.RelicPools, static pool_ => pool_.Relics,
                relic_ => !run_.Player.HasRelic(relic_));
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了 {config_.RelicCount} 件遗物奖励，但遗物池列表没有可发的遗物，已跳过。");
                return;
            }

            if (candidates.Count < config_.RelicCount)
                Debug.LogWarning($"奖励配置 {config_.name} 要 {config_.RelicCount} 件遗物，" +
                                 $"但池里只剩 {candidates.Count} 件未持有，按实际数量发。");

            List<SO_Relic> taken = rng_.TakeShuffled(candidates, config_.RelicCount);
            for (int i = 0; i < taken.Count; i++) rewards_.Add(RunReward.RelicOf(taken[i]));
        }
    }
}
