using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>遭遇奖励掷骰：把奖励配置 + 整局状态掷成一份奖励清单，是"配置 → 面板条目"的唯一入口。
    /// 掷骰顺序固定（金币 → 卡牌各组 → 药水 → 遗物）且全部走同一条整局随机流，同种子可复现；
    /// 三类奖励都支持配置多份池，掷骰前先把池列表合并成一份带权候选集（同引用去重、按池权重赋权）再加权无放回抽取，卡牌候选按配置概率直接以升级形态抽出；
    /// 配置为空（Boss）或卡/药水候选集为空时静默跳过该条，池里断引用的项也直接跳过——绝不让结算抛异常卡住流程；
    /// 遗物候选不足时由整局兜底遗物补齐到配置件数，绝不空手离场。
    /// 配置缺口经调用方注入的 onWarning_ 上报，域层不直接依赖引擎日志出口。</summary>
    public static class EncounterRewardRoller
    {
        /// <summary>每组卡牌奖励的候选张数：恒为三选一，不随配置变化。</summary>
        private const int CardOptionCount = 3;

        #region 掷出一份奖励清单：配置为空时返回空表

        public static List<RunReward> Roll(SO_EncounterReward config_, RunState run_, Action<string> onWarning_ = null)
        {
            List<RunReward> rewards = new();
            if (config_ == null || run_ == null) return rewards;
            Rng rng = run_.Rng;
            run_.SetRewardRngState(rng.State); // 领奖中写盘据此把流设回掷骰前重掷，保证退出前后奖励一致

            RollGold(config_, rng, rewards);
            RollCards(config_, rng, rewards, onWarning_);
            RollPotion(config_, rng, rewards, onWarning_);
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
        private static void RollCards(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_, Action<string> onWarning_)
        {
            if (config_.CardRewardCount <= 0) return;
            List<WeightedItem<SO_Card>> cards = CollectWeighted(config_.CardPools, static pool_ => pool_.Cards,
                static pool_ => pool_.Weight);
            if (cards.Count == 0)
            {
                onWarning_?.Invoke($"奖励配置 {config_.name} 配了 {config_.CardRewardCount} 组卡牌奖励，但卡池列表没有有效卡，已跳过。");
                return;
            }

            for (int i = 0; i < config_.CardRewardCount; i++)
            {
                List<CardModel> options = DrawCards(config_, cards, rng_);
                if (options.Count > 0) rewards_.Add(RunReward.CardsOf(options));
            }
        }

        // 合并多份池成一份带权候选集：断引用的项跳过，同引用只收一次——同一项在两份池里不会重复出现；
        // 每件权重 = 池权重 / 本池贡献件数，单次抽取等价"按池权重轮盘选池、池内等权"。
        // internal 供商店货架合并同用（GameRun 内共享），不对外暴露
        internal static List<WeightedItem<T>> CollectWeighted<T, TPool>(
            IReadOnlyList<TPool> pools_, Func<TPool, IReadOnlyList<T>> items_, Func<TPool, float> weightOf_)
        {
            List<WeightedItem<T>> collected = new();
            HashSet<T> seen = new();
            for (int i = 0; i < pools_.Count; i++)
            {
                TPool pool = pools_[i];
                if (pool == null) continue;
                IReadOnlyList<T> items = items_(pool);
                List<T> fresh = null;
                for (int j = 0; j < items.Count; j++)
                {
                    T item = items[j];
                    if (item != null && seen.Add(item)) (fresh ??= new()).Add(item);
                }

                if (fresh == null) continue;
                float perItem = weightOf_(pool) / fresh.Count;
                for (int j = 0; j < fresh.Count; j++) collected.Add(new WeightedItem<T>(fresh[j], perItem));
            }

            return collected;
        }

        // 不放回抽样：候选集按池权重轮盘逐张取出，保证同一组里不会出现重复项；
        // 每张按升级概率掷骰（概率为 0 或卡不可升级时不掷、不消耗随机流），命中即以升级形态产出——
        // canonical 原型禁止原地升级，须先克隆成可变副本再应用补丁
        private static List<CardModel> DrawCards(SO_EncounterReward config_, List<WeightedItem<SO_Card>> cards_, Rng rng_)
        {
            List<SO_Card> taken = rng_.TakeWeighted(cards_, CardOptionCount);
            List<CardModel> options = new(taken.Count);
            for (int i = 0; i < taken.Count; i++)
            {
                CardModel card = CardModelBaker.Bake(taken[i]);
                if (card.CanUpgrade && config_.CardUpgradeChance > 0f && rng_.Next(0f, 1f) < config_.CardUpgradeChance)
                {
                    card = card.ToMutable();
                    card.Upgrade();
                }

                options.Add(card);
            }

            return options;
        }

        // 药水：先掷概率再按池权重抽一瓶；合并后的候选集为空视为没有药水奖励
        private static void RollPotion(SO_EncounterReward config_, Rng rng_, List<RunReward> rewards_, Action<string> onWarning_)
        {
            if (config_.PotionChance <= 0f) return;
            List<WeightedItem<SO_Potion>> potions = CollectWeighted(config_.PotionPools, static pool_ => pool_.Potions,
                static pool_ => pool_.Weight);
            if (potions.Count == 0)
            {
                onWarning_?.Invoke($"奖励配置 {config_.name} 配了药水概率，但药水池列表没有有效药水，已跳过。");
                return;
            }

            if (rng_.Next(0f, 1f) >= config_.PotionChance) return;
            rewards_.Add(RunReward.PotionOf(rng_.TakeWeighted(potions, 1)[0]));
        }

        // 遗物：抽取全交给 RelicRoller（常规池跳过已持有，抽空自动降级到兜底池补齐）
        private static void RollRelics(SO_EncounterReward config_, RunState run_, Rng rng_, List<RunReward> rewards_)
        {
            if (config_.RelicCount <= 0) return;
            List<SO_Relic> taken = RelicRoller.Roll(run_, config_.RelicPools, config_.RelicCount, rng_);
            for (int i = 0; i < taken.Count; i++) rewards_.Add(RunReward.RelicOf(taken[i]));
        }
    }
}
