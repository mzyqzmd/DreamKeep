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
            List<SO_Card> cards = CollectCards(config_.CardPools);
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

        // 合并卡池：同引用只收一次，避免同一张卡在两份池里时一组候选出现重复
        private static List<SO_Card> CollectCards(IReadOnlyList<SO_CardPool> pools_)
        {
            List<SO_Card> cards = new();
            HashSet<SO_Card> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                SO_CardPool pool = pools_[i];
                if (pool == null) continue;
                for (int j = 0; j < pool.Cards.Count; j++)
                {
                    SO_Card card = pool.Cards[j];
                    if (card != null && seen.Add(card)) cards.Add(card);
                }
            }

            return cards;
        }

        // 不放回抽样：对候选集局部洗牌取前 N 个，保证同一组里不会出现重复的卡
        private static List<CardModel> DrawCards(List<SO_Card> cards_, Rng rng_)
        {
            int take = Math.Min(CardOptionCount, cards_.Count);
            List<CardModel> options = new(take);
            for (int i = 0; i < take; i++)
            {
                int j = rng_.Next(i, cards_.Count);
                (cards_[i], cards_[j]) = (cards_[j], cards_[i]);
                options.Add(CardModelBaker.Bake(cards_[i]));
            }

            return options;
        }

        // 药水：先掷概率再抽一瓶；合并后的候选集为空视为没有药水奖励
        private static void RollPotion(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.PotionChance <= 0f) return;
            List<SO_Potion> potions = CollectPotions(config_.PotionPools);
            if (potions.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了药水概率，但药水池列表没有有效药水，已跳过。");
                return;
            }

            if (rng_.Next(0f, 1f) >= config_.PotionChance) return;
            rewards_.Add(RunReward.PotionOf(potions[rng_.Next(potions.Count)]));
        }

        private static List<SO_Potion> CollectPotions(IReadOnlyList<SO_PotionPool> pools_)
        {
            List<SO_Potion> potions = new();
            HashSet<SO_Potion> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                SO_PotionPool pool = pools_[i];
                if (pool == null) continue;
                for (int j = 0; j < pool.Potions.Count; j++)
                {
                    SO_Potion potion = pool.Potions[j];
                    if (potion != null && seen.Add(potion)) potions.Add(potion);
                }
            }

            return potions;
        }

        // 遗物：合并池后跳过已持有与断引用的项，再不放回抽取；不够件数时按实际数量发，并留一条警告
        private static void RollRelics(SO_EncounterReward config_, RunState run_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.RelicCount <= 0) return;
            List<SO_Relic> candidates = CollectRelics(config_.RelicPools, run_.Player);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了 {config_.RelicCount} 件遗物奖励，但遗物池列表没有可发的遗物，已跳过。");
                return;
            }

            int take = Math.Min(config_.RelicCount, candidates.Count);
            if (take < config_.RelicCount)
                Debug.LogWarning($"奖励配置 {config_.name} 要 {config_.RelicCount} 件遗物，" +
                                 $"但池里只剩 {candidates.Count} 件未持有，按实际数量发。");

            for (int i = 0; i < take; i++)
            {
                int j = rng_.Next(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                rewards_.Add(RunReward.RelicOf(candidates[i]));
            }
        }

        private static List<SO_Relic> CollectRelics(IReadOnlyList<SO_RelicPool> pools_, Player player_)
        {
            List<SO_Relic> relics = new();
            HashSet<SO_Relic> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                SO_RelicPool pool = pools_[i];
                if (pool == null) continue;
                for (int j = 0; j < pool.Relics.Count; j++)
                {
                    SO_Relic relic = pool.Relics[j];
                    if (relic == null || player_.HasRelic(relic) || !seen.Add(relic)) continue;
                    relics.Add(relic);
                }
            }

            return relics;
        }
    }
}
