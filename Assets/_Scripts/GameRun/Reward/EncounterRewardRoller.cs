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
    /// 配置为空（Boss）或池为空时静默跳过该条，池里断引用的项也直接跳过——绝不让结算抛异常卡住流程。</summary>
    public static class EncounterRewardRoller
    {
        /// <summary>每组卡牌奖励的候选张数：恒为三选一，不随配置变化。</summary>
        public const int CardOptionCount = 3;

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

        // 卡牌：每组独立抽三张不重复的候选；池里不足三张时有几张给几张
        private static void RollCards(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.CardRewardCount <= 0) return;
            SO_CardPool pool = config_.CardPool;
            if (pool == null || pool.Cards.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了 {config_.CardRewardCount} 组卡牌奖励但没配卡池，已跳过。");
                return;
            }

            for (int i = 0; i < config_.CardRewardCount; i++)
            {
                List<CardModel> options = DrawCards(pool, rng_);
                if (options.Count > 0) rewards_.Add(RunReward.CardsOf(options));
            }
        }

        // 不放回抽样：先滤掉断引用的池项，再局部洗牌取前 N 个，保证同一组里不会出现重复的卡
        private static List<CardModel> DrawCards(SO_CardPool pool_, Rng rng_)
        {
            List<SO_Card> cards = new(pool_.Cards.Count);
            for (int i = 0; i < pool_.Cards.Count; i++)
            {
                SO_Card card = pool_.Cards[i];
                if (card != null) cards.Add(card);
            }

            int take = Math.Min(CardOptionCount, cards.Count);
            List<CardModel> options = new(take);
            for (int i = 0; i < take; i++)
            {
                int j = rng_.Next(i, cards.Count);
                (cards[i], cards[j]) = (cards[j], cards[i]);
                options.Add(CardModelBaker.Bake(cards[i]));
            }

            return options;
        }

        // 药水：先掷概率再抽一瓶；池空（或全是断引用）视为没有药水奖励
        private static void RollPotion(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.PotionChance <= 0f) return;
            SO_PotionPool pool = config_.PotionPool;
            if (pool == null || pool.Potions.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了药水概率但没配药水池，已跳过。");
                return;
            }

            List<SO_Potion> potions = new(pool.Potions.Count);
            for (int i = 0; i < pool.Potions.Count; i++)
            {
                SO_Potion potion = pool.Potions[i];
                if (potion != null) potions.Add(potion);
            }

            if (potions.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 的药水池里没有有效药水，已跳过。");
                return;
            }

            if (rng_.Next(0f, 1f) >= config_.PotionChance) return;
            rewards_.Add(RunReward.PotionOf(potions[rng_.Next(potions.Count)]));
        }

        // 遗物：跳过玩家已持有与断引用的项后不放回抽取；不够件数时按实际数量发，并留一条警告
        private static void RollRelics(SO_EncounterReward config_, RunState run_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.RelicCount <= 0) return;
            SO_RelicPool pool = config_.RelicPool;
            if (pool == null || pool.Relics.Count == 0)
            {
                Debug.LogWarning($"奖励配置 {config_.name} 配了 {config_.RelicCount} 件遗物奖励但没配遗物池，已跳过。");
                return;
            }

            List<SO_Relic> candidates = new(pool.Relics.Count);
            for (int i = 0; i < pool.Relics.Count; i++)
            {
                SO_Relic relic = pool.Relics[i];
                if (relic == null || run_.Player.HasRelic(relic)) continue;
                candidates.Add(relic);
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
    }
}
